import csv
import json
import os
import re
import threading
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

with (ROOT / "data" / "finance_terms.csv").open(encoding="utf-8-sig", newline="") as source:
    DEFINITIONS = {row["term"].lower(): row["definition"] for row in csv.DictReader(source)}

TRAINED_DEFINITIONS = json.loads((ROOT / "dictionary.json").read_text(encoding="utf-8"))
DEFINITIONS.update(TRAINED_DEFINITIONS)


def grounded_example(story: str, hints: list[str], term: str) -> str:
    sentences = [item.strip() for item in re.split(r"(?<=[.!?])\s+", story) if item.strip()]
    direct = [item for item in sentences if term.casefold() in item.casefold()]
    if direct:
        return direct[0]
    lowered_hints = [hint.casefold() for hint in hints if isinstance(hint, str)]
    for sentence in sentences:
        if any(hint and hint in sentence.casefold() for hint in lowered_hints):
            return sentence
    return ""


class TermService:
    def __init__(self):
        self.lock = threading.Lock()
        self.model = None
        self.tokenizer = None
        self.backend = "dictionary"

    @property
    def model_available(self) -> bool:
        model_root = ROOT / "models"
        return (model_root / "terms" / "config.json").is_file() or (model_root / "adapter" / "adapter_config.json").is_file()

    def load(self):
        if self.model is not None:
            return
        if not self.model_available:
            raise RuntimeError("Local term model files are not installed")
        import torch
        from transformers import AutoModelForSeq2SeqLM, AutoTokenizer

        torch.set_num_threads(min(4, os.cpu_count() or 1))
        path = ROOT / "models" / "terms"
        if (path / "config.json").is_file():
            tokenizer = AutoTokenizer.from_pretrained(path, local_files_only=True)
            model = AutoModelForSeq2SeqLM.from_pretrained(path, local_files_only=True)
            backend = "local_merged"
        else:
            from peft import PeftModel

            adapter = ROOT / "models" / "adapter"
            config = json.loads((adapter / "adapter_config.json").read_text(encoding="utf-8"))
            tokenizer = AutoTokenizer.from_pretrained(adapter, local_files_only=True)
            base = AutoModelForSeq2SeqLM.from_pretrained(config["base_model_name_or_path"])
            model = PeftModel.from_pretrained(base, adapter, local_files_only=True).merge_and_unload()
            backend = "trained_adapter"
        model.eval()
        self.torch = torch
        self.tokenizer = tokenizer
        self.model = model
        self.backend = backend

    def explain(self, term: str, story: str, hints: list[str], weights: dict[str, float]) -> dict:
        definition = DEFINITIONS.get(term.casefold(), "").rstrip(".")
        if not definition:
            return {"term": term, "definition": "", "example": "", "text": ""}
        use_model = os.getenv("FINSTORY_TERM_MODEL", "0") == "1" and term in TRAINED_DEFINITIONS and self.model_available
        if use_model:
            with self.lock:
                self.load()
                interests = ", ".join(f"{key}: {value:.2f}" for key, value in weights.items())
                prompt = (
                    "Объясни термин простыми словами и приведи пример только из истории. "
                    f"Термин: {term}. Значение: {definition}. Интересы: {interests}. История: {story} Объяснение:"
                )
                inputs = self.tokenizer(prompt, return_tensors="pt", truncation=True, max_length=384)
                with self.torch.inference_mode():
                    output = self.model.generate(**inputs, max_new_tokens=192, num_beams=3, do_sample=False)
                text = self.tokenizer.decode(output[0], skip_special_tokens=True).strip()
                if text:
                    return {"term": term, "definition": "", "example": "", "text": text}
        example = grounded_example(story, hints, term)
        text = definition + "." + ((" " + example) if example else "")
        return {"term": term, "definition": definition + ".", "example": example, "text": text}


TERMS = TermService()

