import os
import logging
from typing import Annotated

from fastapi import Depends, FastAPI, Header, HTTPException
from pydantic import BaseModel, Field, field_validator

from .agent import DemoAgent, StoryAgent
from .interests import InterestModel, LABELS
from .terms import DEFINITIONS, TERMS

SERVICE_VERSION = "1.0.0"
log = logging.getLogger(__name__)
app = FastAPI(title="FinStory Internal API", version=SERVICE_VERSION, docs_url=None, redoc_url=None)


class Character(BaseModel):
    name: str = Field(min_length=1, max_length=40)
    age: int = Field(ge=12, le=25)
    personality: str = Field(min_length=1, max_length=120)


class Mistake(BaseModel):
    question: str = Field(min_length=1, max_length=500)
    userAnswer: str = Field(min_length=1, max_length=300)
    correctAnswer: str = Field(min_length=1, max_length=300)
    explanation: str = Field(default="", max_length=700)


class InterestRequest(BaseModel):
    text: str = Field(min_length=1, max_length=800)


class InterestResponse(BaseModel):
    primaryInterest: str
    interests: list[str]
    weights: dict[str, float]


class LessonRequest(BaseModel):
    requestId: str = Field(min_length=1, max_length=100)
    topicId: str = Field(min_length=1, max_length=100)
    title: str = Field(min_length=1, max_length=120)
    learningGoal: str = Field(min_length=1, max_length=700)
    verifiedMaterial: str = Field(min_length=1, max_length=2000)
    verifiedExample: str = Field(min_length=1, max_length=1000)
    interestText: str = Field(default="", max_length=800)
    interestWeights: dict[str, float] = Field(default_factory=dict)
    mistakes: list[Mistake] = Field(default_factory=list, max_length=10)
    recentCorrect: int = Field(default=0, ge=0, le=1000)
    recentTotal: int = Field(default=0, ge=0, le=1000)
    previousStory: str = Field(default="", max_length=1200)
    character: Character | None = None
    terms: list[str] = Field(default_factory=list, max_length=8)

    @field_validator("interestWeights")
    @classmethod
    def valid_weights(cls, value: dict[str, float]):
        if any(weight < 0 or weight > 1 for weight in value.values()):
            raise ValueError("interest weights must be between 0 and 1")
        return value


class TermResponse(BaseModel):
    term: str
    text: str
    definition: str
    example: str


class LessonResponse(BaseModel):
    title: str
    story: str
    takeaway: str
    character: Character
    terms: list[TermResponse]
    cheatSheet: list[str]
    source: str
    modelVersion: str


def require_internal_token(x_internal_token: Annotated[str | None, Header()] = None):
    expected = os.getenv("FINSTORY_INTERNAL_TOKEN", "")
    if expected and x_internal_token != expected:
        raise HTTPException(status_code=401, detail="invalid internal token")


@app.get("/internal/v1/health")
def health():
    yandex = StoryAgent()
    return {
        "status": "ok",
        "version": SERVICE_VERSION,
        "mode": "demo" if os.getenv("FINSTORY_DEMO", "1") == "1" else "yandex",
        "yandexConfigured": yandex.configured,
        "termModelAvailable": TERMS.model_available,
        "termBackend": TERMS.backend,
    }


@app.post(
    "/internal/v1/interests/classify",
    response_model=InterestResponse,
    dependencies=[Depends(require_internal_token)],
)
def classify_interests(request: InterestRequest):
    model = InterestModel()
    weights = model.weights(request.text)
    interests = model.labels(weights)
    return InterestResponse(
        primaryInterest=interests[0], interests=interests, weights=weights
    )


@app.post(
    "/internal/v1/lessons/generate",
    response_model=LessonResponse,
    dependencies=[Depends(require_internal_token)],
)
def generate_lesson(request: LessonRequest):
    interest_model = InterestModel()
    weights = request.interestWeights or interest_model.weights(request.interestText)
    primary_key = next(iter(weights), "other")
    term_names = [name.casefold() for name in request.terms if name.casefold() in DEFINITIONS]
    task = {
        "request_id": request.requestId,
        "topic_id": request.topicId,
        "lesson": request.title,
        "learning_goal": request.learningGoal,
        "verified_material": request.verifiedMaterial,
        "verified_example": request.verifiedExample,
        "interest_weights": weights,
        "primary_interest": LABELS.get(primary_key, primary_key),
        "mistakes": [item.model_dump() for item in request.mistakes],
        "recent_correct": request.recentCorrect,
        "recent_total": request.recentTotal,
        "previous_story": request.previousStory,
        "character": request.character.model_dump() if request.character else None,
        "terms": [{"term": name, "definition": DEFINITIONS[name]} for name in term_names],
    }
    demo = os.getenv("FINSTORY_DEMO", "1") == "1"
    agent = DemoAgent() if demo else StoryAgent()
    if not demo and not agent.configured:
        raise HTTPException(status_code=503, detail="Yandex generation is not configured")
    try:
        result = agent.write(task, term_names)
        terms = [
            TERMS.explain(name, result["story"], result["hints"].get(name, []), weights)
            for name in term_names
        ]
    except (RuntimeError, ValueError, OSError) as exc:
        if demo:
            raise HTTPException(status_code=502, detail="lesson generation failed") from exc
        log.warning("Yandex lesson generation failed; using personalized local fallback", exc_info=True)
        result = DemoAgent(source="local-personalized").write(task, term_names)
        terms = [
            TERMS.explain(name, result["story"], result["hints"].get(name, []), weights)
            for name in term_names
        ]
    cheat_sheet = [request.verifiedMaterial]
    return LessonResponse(
        title=f"{request.title}: персональная история",
        story=result["story"],
        takeaway=result["takeaway"],
        character=Character(**result["character"]),
        terms=[TermResponse(**item) for item in terms],
        cheatSheet=cheat_sheet,
        source=result["source"],
        modelVersion=(StoryAgent().model if result["source"] == "yandex" else result["source"]),
    )
