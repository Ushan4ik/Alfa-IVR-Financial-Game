TOPICS = {
    "cars": ("гонк", "картинг", "formula", "формул", "ferrari", "mclaren", "verstappen"),
    "anime": ("аниме", "наруто", "манга", "ванпис", "one piece", "драгонбол"),
    "art": ("музе", "картин", "пикассо", "рембранд", "галере", "рисован"),
    "architecture": ("архитектур", "макет", "здани", "собор", "памятник"),
    "sport": ("футбол", "хоккей", "баскетбол", "теннис", "волейбол", "фигурн", "карате", "спорт"),
    "music": ("музык", "песн", "гитар", "микрофон", "k-pop", "к-поп"),
    "movies": ("фильм", "кино", "марвел", "гарри поттер", "комеди"),
    "travel": ("путешеств", "поездк", "япон", "стран"),
    "books": ("книг", "чита", "роман"),
    "gaming": ("компьютерн", "игр", "minecraft"),
    "cooking": ("готов", "кулинар", "печь", "выпеч"),
    "crafts": ("вышив", "вяза", "рукодел"),
}

LABELS = {
    "cars": "автомобили", "anime": "аниме", "art": "рисование",
    "architecture": "архитектура", "sport": "спорт", "music": "музыка",
    "movies": "кино", "travel": "путешествия", "books": "чтение",
    "gaming": "игры", "cooking": "кулинария", "crafts": "рукоделие",
    "other": "другое",
}


class InterestModel:
    """Deterministic first-stage classifier. It is intentionally cheap and predictable."""

    def weights(self, answer: str) -> dict[str, float]:
        text = answer.casefold()
        counts = {
            topic: sum(text.count(fragment) for fragment in fragments)
            for topic, fragments in TOPICS.items()
        }
        counts = {topic: count for topic, count in counts.items() if count > 0}
        if not counts:
            return {"other": 1.0}
        ranked = sorted(counts.items(), key=lambda item: (-item[1], item[0]))[:5]
        total = sum(value for _, value in ranked)
        return {topic: round(value / total, 4) for topic, value in ranked}

    def labels(self, weights: dict[str, float]) -> list[str]:
        return [LABELS.get(key, key) for key in weights]

