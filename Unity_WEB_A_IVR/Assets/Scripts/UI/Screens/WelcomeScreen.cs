using IVR.Api;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public class WelcomeScreen : BaseScreen
    {
        private static readonly string[] TourTitles =
        {
            "Двигайся по дорожке знаний",
            "Коины — твоя валюта",
            "Обменивай монеты на реальные бонусы",
            "Заходи каждый день — держи серию",
            "Всё готово!"
        };

        private static readonly string[] TourCopies =
        {
            "Каждый шаг — новый урок о банковских продуктах. Карты, вклады, кредиты — всё в игровом формате.",
            "Проходи темы, зарабатывай коины и обменивай их на крутые фичи.",
            "Повышенный кешбэк, скидки в кофейнях и другие призы ждут тебя.",
            "Возвращайся каждый день, сохраняй серию и соревнуйся с друзьями в рейтинге.",
            "Первое задание уже ждёт — получи награду за старт."
        };

        private static readonly string[] TourSymbols = { "★", "A", "🎁", "🔥", "😎" };
        private int _tourSlide;

        public WelcomeScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            Root.Q<Button>("start-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Register);
            Root.Q<Button>("login-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Login);
            Root.Q<Button>("back-game-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Hub);
            Root.Q<Button>("tour-back").clicked += PreviousTourSlide;
            Root.Q<Button>("tour-next").clicked += NextTourSlide;
        }

        public new void Show()
        {
            base.Show();
            var returningPlayer = IvrApi.Auth.IsLoggedIn;
            Root.Q<ScrollView>("welcome-entry").style.display = returningPlayer ? DisplayStyle.None : DisplayStyle.Flex;
            Root.Q<VisualElement>("welcome-tour").style.display = returningPlayer ? DisplayStyle.Flex : DisplayStyle.None;
            Root.Q<Button>("start-btn").style.display = returningPlayer ? DisplayStyle.None : DisplayStyle.Flex;
            Root.Q<Button>("login-btn").style.display = returningPlayer ? DisplayStyle.None : DisplayStyle.Flex;
            Root.Q<Button>("back-game-btn").style.display = DisplayStyle.None;
            if (returningPlayer) ShowTourSlide(0);
        }

        private void PreviousTourSlide()
        {
            if (_tourSlide == 0) ScreenManager.Instance.Show(AppScreen.Hub);
            else ShowTourSlide(_tourSlide - 1);
        }

        private void NextTourSlide()
        {
            if (_tourSlide == TourTitles.Length - 1) ScreenManager.Instance.Show(AppScreen.Hub);
            else ShowTourSlide(_tourSlide + 1);
        }

        private void ShowTourSlide(int index)
        {
            _tourSlide = index;
            Root.Q<Label>("tour-step").text = $"{index + 1} из {TourTitles.Length}";
            Root.Q<VisualElement>("tour-progress-fill").style.width = Length.Percent((index + 1) * 20f);
            Root.Q<Label>("tour-symbol").text = TourSymbols[index];
            Root.Q<Label>("tour-title").text = TourTitles[index];
            Root.Q<Label>("tour-copy").text = TourCopies[index];
            Root.Q<Button>("tour-back").text = index == 0 ? "В игру" : "← Назад";
            Root.Q<Button>("tour-next").text = index == TourTitles.Length - 1 ? "Начать →" : "Продолжить →";
        }
    }
}
