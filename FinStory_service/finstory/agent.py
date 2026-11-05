import json
import os
import re
from urllib.request import Request, urlopen


def safe_hints(story: str, terms: list[str], raw) -> dict[str, list[str]]:
    found = {
        item.get("term"): item.get("keywords", [])
        for item in raw if isinstance(item, dict)
    } if isinstance(raw, list) else {}
    tokens = set(re.findall(r"[а-яёa-z0-9]+", story.casefold()))
    hints: dict[str, list[str]] = {}
    sentences = [item.strip() for item in re.split(r"(?<=[.!?])\s+", story) if item.strip()]
    for term in terms:
        accepted = []
        for phrase in found.get(term, []):
            if not isinstance(phrase, str) or len(phrase) > 100:
                continue
            words = re.findall(r"[а-яёa-z0-9]+", phrase.casefold())
            required = [word for word in words if len(word) > 2 or word.isdigit()]
            if required and all(word in tokens for word in required):
                accepted.append(phrase.strip())
        if not accepted:
            accepted = [sentence for sentence in sentences if term.casefold() in sentence.casefold()]
        hints[term] = (accepted or sentences)[:1]
    return hints


class StoryAgent:
    def __init__(self, opener=urlopen):
        self.key = os.getenv("YANDEX_API_KEY", "")
        self.folder = os.getenv("YANDEX_FOLDER_ID", "")
        self.model = os.getenv("FINSTORY_YANDEX_MODEL", "aliceai-llm")
        self.opener = opener

    @property
    def configured(self) -> bool:
        return bool(self.key and self.folder)

    def ask(self, task: dict, schema: dict) -> dict:
        payload = {
            "model": self.model if self.model.startswith("gpt://") else f"gpt://{self.folder}/{self.model}",
            "instructions": (
                "Пиши связную реалистичную историю финансового выбора для подростка. "
                "Используй только проверенный материал из входных данных. Интересы влияют на детали, "
                "а ошибки пользователя — на то, какой выбор нужно разобрать. Сохраняй героя и прошлые события. "
                "Глава должна содержать цель, ограничение, выбор и последствие. Не выдумывай реальные банковские "
                "ставки, законы и условия продуктов. Для терминов верни только факты, прямо присутствующие в истории. "
                "Не выводи рассуждения. Возвращай только JSON."
            ),
            "input": json.dumps(task, ensure_ascii=False),
            "temperature": 0.4,
            "max_output_tokens": 1100,
            "text": {"format": {"type": "json_schema", "name": "finstory_lesson", "strict": True, "schema": schema}},
        }
        request = Request(
            "https://ai.api.cloud.yandex.net/v1/responses",
            data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
            headers={
                "Authorization": "Api-Key " + self.key,
                "x-folder-id": self.folder,
                "Content-Type": "application/json",
            },
        )
        with self.opener(request, timeout=45) as response:
            data = json.load(response)
        output = "".join(
            part.get("text", "")
            for item in data.get("output", [])
            for part in item.get("content", [])
            if part.get("type") == "output_text"
        )
        if not output:
            raise ValueError("Generation returned an empty response")
        return json.loads(output)

    def write(self, task: dict, term_names: list[str]) -> dict:
        if not self.configured:
            raise RuntimeError("Yandex generation is not configured")
        schema = {
            "type": "object",
            "properties": {
                "story": {"type": "string"},
                "takeaway": {"type": "string"},
                "character": {
                    "type": "object",
                    "properties": {
                        "name": {"type": "string"},
                        "age": {"type": "integer"},
                        "personality": {"type": "string"},
                    },
                    "required": ["name", "age", "personality"],
                    "additionalProperties": False,
                },
                "hints": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "term": {"type": "string", "enum": term_names},
                            "keywords": {"type": "array", "items": {"type": "string"}},
                        },
                        "required": ["term", "keywords"],
                        "additionalProperties": False,
                    },
                },
            },
            "required": ["story", "takeaway", "character", "hints"],
            "additionalProperties": False,
        }
        result = self.ask(task, schema)
        story = result["story"].strip()
        takeaway = result["takeaway"].strip()
        character = result["character"]
        if not 120 <= len(story) <= 1200:
            raise ValueError("Generated story has invalid length")
        if not takeaway or len(takeaway) > 400:
            raise ValueError("Generated takeaway has invalid length")
        expected_character = task.get("character")
        if expected_character and character != expected_character:
            raise ValueError("Character continuity was not preserved")
        return {
            "story": story,
            "takeaway": takeaway,
            "character": character,
            "hints": safe_hints(story, term_names, result.get("hints", [])),
            "source": "yandex",
        }


class DemoAgent:
    def write(self, task: dict, term_names: list[str]) -> dict:
        character = task.get("character") or {
            "name": "Мира", "age": 17, "personality": "внимательная"
        }
        interest = task.get("primary_interest", "своими увлечениями")
        example = str(task.get("verified_example", "")).strip()
        mistakes = task.get("mistakes", [])
        mistake_line = ""
        if mistakes:
            item = mistakes[0]
            mistake_line = f" Раньше {character['name']} выбрала вариант «{item.get('userAnswer', '')}», а теперь проверила расчёт ещё раз."
        story = (
            f"{character['name']} увлекается {interest} и решил(а) привести личные финансы в порядок. "
            f"{example}{mistake_line} После сравнения вариантов был выбран более понятный и посильный план."
        )[:1200]
        return {
            "story": story,
            "takeaway": str(task.get("verified_material", ""))[:400],
            "character": character,
            "hints": safe_hints(story, term_names, []),
            "source": "demo",
        }

