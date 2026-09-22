using System.Threading.Tasks;
using IVR.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace IVR.Game
{
    [RequireComponent(typeof(QuizManager))]
    public class QuizUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private QuizManager _quiz;

        private Label    _progressText;
        private Label    _scoreText;
        private Label    _questionText;
        private Label    _feedbackText;
        private Label    _progressPercent;
        private Label    _questionNumber;
        private VisualElement _progressFill;
        private ScrollView _scroll;
        private Button   _nextBtn;
        private Button[] _answerBtns;

        private VisualElement _resultsOverlay;
        private VisualElement _pauseOverlay;

        private int _score;
        private int _correctCount;
        private bool _answered;
        private bool _answerBusy;
        private bool _disposed;

        private static readonly string[] AnswerLetters = { "А", "Б", "В", "Г" };

        private void OnDestroy() => _disposed = true;

        private void Start()
        {
            _quiz = GetComponent<QuizManager>();

            if (uiDocument == null) { Debug.LogError("UIDocument не назначен!"); return; }

            var root = uiDocument.rootVisualElement;

            _progressText = root.Q<Label>("progress-text");
            _scoreText    = root.Q<Label>("score-text");
            _questionText = root.Q<Label>("question-text");
            _feedbackText = root.Q<Label>("feedback-text");
            _progressPercent = root.Q<Label>("progress-percent");
            _questionNumber = root.Q<Label>("question-number");
            _progressFill = root.Q<VisualElement>("quiz-progress-fill");
            _scroll = root.Q<ScrollView>("quiz-scroll");
            _nextBtn      = root.Q<Button>("next-btn");
            _resultsOverlay = root.Q<VisualElement>("results-overlay");
            _pauseOverlay = root.Q<VisualElement>("pause-overlay");

            var topic = "Финансовый квиз";
            var config = GameManager.Instance?.PendingQuiz;
            if (config?.IsDiagnostic == true) topic = "Вводный тест";
            if (!string.IsNullOrEmpty(config?.LessonId))
            {
                foreach (var lesson in LessonCatalog.All)
                    if (lesson.Id == config.LessonId) { topic = lesson.Title; break; }
            }
            else if (!string.IsNullOrEmpty(config?.Category))
                topic = config.Category;
            root.Q<Label>("quiz-topic").text = topic;

            _answerBtns = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                _answerBtns[i] = root.Q<Button>($"answer-{i}");
                _answerBtns[i].clicked += () => OnAnswer(index);
            }

            _nextBtn.clicked += OnNext;
            root.Q<Button>("exit-btn").clicked += () => _pauseOverlay.style.display = DisplayStyle.Flex;
            root.Q<Button>("pause-resume-btn").clicked += () => _pauseOverlay.style.display = DisplayStyle.None;
            root.Q<Button>("pause-exit-btn").clicked += () => UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            root.Q<Button>("result-continue-btn").clicked += OnContinue;

            _ = LoadAndStart();
        }

        private async Task LoadAndStart()
        {
            _questionText.text = "Загружаю вопросы...";
            foreach (var button in _answerBtns) button.style.display = DisplayStyle.None;
            try
            {
                await _quiz.LoadFromServer();
            }
            catch (System.Exception error)
            {
                Debug.LogException(error);
            }

            if (_disposed) return;

            if (!_quiz.IsLoaded)
            {
                _questionText.text = "Не удалось загрузить вопросы. Вернись в меню и попробуй ещё раз.";
                _progressText.text = "Вопросы недоступны";
                return;
            }
            if (_quiz.IsPractice) uiDocument.rootVisualElement.Q<Label>("quiz-topic").text = "Тренировка";
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            var q = _quiz.Current;
            _answered = false;

            int number = _quiz.CurrentIndex + 1;
            int percent = Mathf.RoundToInt(100f * number / _quiz.Total);
            _progressText.text = $"Вопрос {number} из {_quiz.Total}";
            _progressPercent.text = percent + "%";
            _progressFill.style.width = Length.Percent(percent);
            _questionNumber.text = $"ВОПРОС {number:00}";
            _scoreText.text    = $"{_score} очков";
            _questionText.text = q.text;
            _feedbackText.text = _quiz.IsPractice ? _quiz.PracticeReason : "";

            for (int i = 0; i < _answerBtns.Length; i++)
            {
                bool available = i < q.answers.Count;
                _answerBtns[i].text = available ? AnswerLetters[i] + "   " + q.answers[i] : "";
                _answerBtns[i].style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                _answerBtns[i].SetEnabled(true);
                _answerBtns[i].pickingMode = PickingMode.Position;
                _answerBtns[i].RemoveFromClassList("answer-correct");
                _answerBtns[i].RemoveFromClassList("answer-wrong");
            }

            _nextBtn.style.display = DisplayStyle.None;
            _scroll.scrollOffset = Vector2.zero;
        }

        private async void OnAnswer(int index)
        {
            if (_answerBusy || _answered || !_quiz.IsLoaded || index >= _quiz.Current.answers.Count) return;
            _answerBusy = true;
            _feedbackText.text = "Проверяем ответ...";
            IVR.Api.ApiResult<IVR.Models.LearningAnswerDto> response;
            try { response = await _quiz.Answer(index); }
            catch (System.Exception) { response = IVR.Api.ApiResult<IVR.Models.LearningAnswerDto>.Fail("Нет связи"); }
            finally { _answerBusy = false; }
            if (_disposed) return;
            if (!response.Success)
            {
                _feedbackText.text = "Не удалось подтвердить ответ. Нажми вариант ещё раз — повторим прежний выбор.";
                return;
            }
            index = _quiz.SelectedIndex(index);
            _answered = true;
            bool correct = response.Data.correct;
            _quiz.Current.correctIndex = response.Data.correctIndex;

            if (correct)
            {
                _score += 10;
                _correctCount++;
                _feedbackText.text = "Верно! +10 очков";
                _feedbackText.style.color = new StyleColor(new Color(0.19f, 0.55f, 0.18f));
                _answerBtns[index].AddToClassList("answer-correct");
            }
            else
            {
                _feedbackText.text = "Не совсем. Правильный ответ выделен зелёным.";
                _feedbackText.style.color = new StyleColor(new Color(0.75f, 0.12f, 0.20f));
                _answerBtns[index].AddToClassList("answer-wrong");
                int correctIndex = _quiz.Current.correctIndex;
                if (correctIndex >= 0 && correctIndex < _answerBtns.Length)
                    _answerBtns[correctIndex].AddToClassList("answer-correct");
            }

            _feedbackText.text += "\n" + response.Data.explanation;
            _scoreText.text = $"{_score} очков";

            foreach (var btn in _answerBtns)
                btn.pickingMode = PickingMode.Ignore;

            _nextBtn.text = _quiz.HasNext() ? "Следующий вопрос →" : "Посмотреть результат →";
            _nextBtn.style.display = DisplayStyle.Flex;
        }

        private void ShowResults()
        {
            bool passed = _correctCount >= Mathf.CeilToInt(_quiz.Total / 2f);

            var root = uiDocument.rootVisualElement;
            root.Q<Label>("result-emoji").text   = passed ? "★" : "↗";
            root.Q<Label>("result-title").text   = passed ? "Отличная работа!" : "Попробуй ещё раз";
            root.Q<Label>("result-correct").text = $"{_correctCount}/{_quiz.Total}";
            root.Q<Label>("result-score").text   = $"+{_score}";
            root.Q<Label>("result-status").text  = passed ? "Пройдено ✓" : "Не пройдено";
            root.Q<Label>("result-status").style.color = passed
                ? new StyleColor(new Color(0.19f, 0.55f, 0.18f))
                : new StyleColor(new Color(0.75f, 0.12f, 0.20f));
            root.Q<Label>("result-message").text = passed
                ? "Ты ответил верно как минимум на половину вопросов. Так держать!"
                : "Не расстраивайся — повтори материал и попробуй снова.";

            root.Q<Label>("result-message").text += _quiz.IsPractice
                ? "\nТренировка не сохраняется в профиль."
                : "\nПосле сохранения открой персональный разбор в уроках. Очки за уже освоенные вопросы повторно не начисляются.";
            _resultsOverlay.style.display = DisplayStyle.Flex;
        }

        private async void OnContinue()
        {
            var button = uiDocument.rootVisualElement.Q<Button>("result-continue-btn");
            button.SetEnabled(false);
            button.text = "Сохраняем результат...";
            bool saved;
            try { saved = await _quiz.Finish(_score); }
            catch (System.Exception) { saved = false; }
            if (!saved && !_disposed)
            {
                button.SetEnabled(true);
                button.text = "Повторить сохранение →";
                uiDocument.rootVisualElement.Q<Label>("result-message").text =
                    "Нет связи с сервером. Ответы сохранены по ходу квиза; повтори завершение попытки.";
            }
        }

        private void OnNext()
        {
            if (!_answered) return;
            if (_quiz.HasNext())
            {
                _quiz.Next();
                ShowQuestion();
            }
            else
                ShowResults();
        }
    }
}
