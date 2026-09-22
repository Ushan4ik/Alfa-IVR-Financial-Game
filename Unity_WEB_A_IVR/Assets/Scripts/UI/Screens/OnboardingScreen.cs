using System.Threading.Tasks;
using IVR;
using IVR.Game;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public class OnboardingScreen : BaseScreen
    {
        private const int LastStep = 4;

        private static readonly (string title, string description, string symbol)[] Slides =
        {
            ("Двигайся по дорожке знаний", "Каждая тема — небольшой урок и квиз. Начни с накоплений и двигайся дальше в своём темпе.", "01"),
            ("Зарабатывай очки", "За правильные ответы ты получаешь очки финансовой грамотности и повышаешь уровень.", "+10"),
            ("Проверяй знания в игре", "Пройди квиз, а затем попробуй виртуальную биржу и посмотри свой результат в рейтинге.", "₸"),
            ("Достигай целей", "Выполняй задания, открывай темы и возвращайся к тем вопросам, которые хочется повторить.", "✓"),
            ("Расскажи о себе", "Напиши, что тебе интересно. Затем короткий вводный тест поможет подобрать уроки.", "А")
        };

        private TextField _interestsField;
        private Button _confirmButton;
        private int _step;
        private bool _editing;

        public OnboardingScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            _interestsField = Root.Q<TextField>("interests-field");
            _confirmButton = Root.Q<Button>("confirm-btn");
            _confirmButton.clicked += () => _ = Confirm();
            Root.Q<Button>("skip-btn").clicked += () => GoToInterests();
            Root.Q<Button>("back-btn").clicked += () =>
            {
                if (_step > 0) { _step--; Render(); }
            };
        }

        public new void Show()
        {
            base.Show();
            _step = 0;
            _editing = false;
            Render();
        }

        public void GoToInterests(bool editing = false)
        {
            _step = LastStep;
            _editing = editing;
            Render();
        }

        private void Render()
        {
            var slide = Slides[_step];
            Root.Q<Label>("step-text").text = (_step + 1) + " / " + Slides.Length;
            Root.Q<VisualElement>("progress-fill").style.width = Length.Percent(100f * (_step + 1) / Slides.Length);
            Root.Q<Label>("art-symbol").text = slide.symbol;
            Root.Q<Label>("slide-title").text = slide.title;
            Root.Q<Label>("slide-description").text = slide.description;
            Root.Q<VisualElement>("interests-group").style.display = _step == LastStep ? DisplayStyle.Flex : DisplayStyle.None;
            Root.Q<Button>("skip-btn").style.display = _step == LastStep ? DisplayStyle.None : DisplayStyle.Flex;
            Root.Q<Button>("back-btn").SetEnabled(_step > 0);
            _confirmButton.text = _step == LastStep ? (_editing ? "Сохранить →" : "Пройти вводный тест →") : "Продолжить →";
            SetStatus("status-text", "");
        }

        private async Task Confirm()
        {
            if (_step < LastStep)
            {
                _step++;
                Render();
                return;
            }

            if (string.IsNullOrWhiteSpace(_interestsField.value))
            {
                SetStatus("status-text", "Напиши хотя бы пару слов", isError: true);
                return;
            }

            _confirmButton.SetEnabled(false);
            SetStatus("status-text", "Сохраняем интересы...");
            try
            {
                var result = await IvrApi.Player.SetInterests(_interestsField.value.Trim());
                if (!result.Success)
                {
                    SetStatus("status-text", result.Error, isError: true);
                    return;
                }

                var profile = await IvrApi.Player.GetProfile();
                if (profile.Success) GameManager.Instance.SetProfile(profile.Data);
                if (_editing) ScreenManager.Instance.Show(AppScreen.Hub);
                else GameManager.Instance.StartQuiz(QuizConfig.Diagnostic());
            }
            finally
            {
                _confirmButton.SetEnabled(true);
            }
        }
    }
}
