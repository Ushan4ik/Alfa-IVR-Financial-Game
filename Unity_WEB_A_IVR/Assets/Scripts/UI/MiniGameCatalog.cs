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
        public bool IsBuiltIn { get; }

        public bool IsAvailable => IsBuiltIn || !string.IsNullOrEmpty(SceneName);

        public MiniGameDefinition(string id, string title, string description, string duration,
            string status, bool isBuiltIn = false, string sceneName = "")
        {
            Id = id;
            Title = title;
            Description = description;
            Duration = duration;
            Status = status;
            IsBuiltIn = isBuiltIn;
            SceneName = sceneName;
        }
    }

    // Простые игры открываются внутри Hub, крупные можно подключать отдельной сценой через SceneName.
    public static class MiniGameCatalog
    {
        public static readonly IReadOnlyList<MiniGameDefinition> All = new[]
        {
            new MiniGameDefinition(
                "office-clicker",
                "Офис мечты",
                "Нажимай, развивай компанию и подготовь офис к будущим отделам и улучшениям.",
                "Без ограничений",
                "Доступно",
                true),
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
