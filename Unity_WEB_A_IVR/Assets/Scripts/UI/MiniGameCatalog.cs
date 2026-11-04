using System.Collections.Generic;

namespace IVR.UI
{
    public sealed class MiniGameDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Duration { get; }
        public string Status { get; }
        public string SceneName { get; }

        public bool IsAvailable => !string.IsNullOrEmpty(SceneName);

        public MiniGameDefinition(string id, string title, string description, string duration,
            string status, string sceneName = "")
        {
            Id = id;
            Title = title;
            Description = description;
            Duration = duration;
            Status = status;
            SceneName = sceneName;
        }
    }

    // Чтобы подключить игру, добавь сцену в Build Settings и укажи её имя в SceneName.
    public static class MiniGameCatalog
    {
        public static readonly IReadOnlyList<MiniGameDefinition> All = new[]
        {
            new MiniGameDefinition(
                "block-puzzle",
                "Собери капитал",
                "Размещай финансовые блоки, собирай линии и увеличивай виртуальный капитал.",
                "3–5 минут",
                "В разработке"),
            new MiniGameDefinition(
                "budget-basket",
                "Финансовая корзина",
                "Распределяй покупки между необходимым, желаниями и накоплениями.",
                "2–3 минуты",
                "Запланировано"),
            new MiniGameDefinition(
                "term-pairs",
                "Пары терминов",
                "Соединяй финансовые понятия с правильными определениями.",
                "2 минуты",
                "Запланировано")
        };
    }
}
