using System.Collections.Generic;
using System.Threading.Tasks;
using IVR;
using IVR.Api;
using IVR.Game;
using IVR.Models;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    private List<QuestionDto> _questions = new();
    private int _currentIndex;
    private string _attemptId;
    // При неопределённом сетевом ответе повторяем тот же выбор, не разрешая заменить уже принятый ответ.
    private int? _pendingAnswer;
    public QuestionDto Current => _questions[_currentIndex];
    public int Total => _questions.Count;
    public int CurrentIndex => _currentIndex;
    public bool IsLoaded => _questions.Count > 0;
    public bool IsPractice => string.IsNullOrEmpty(_attemptId);
    public string PracticeReason { get; private set; }

    public async Task LoadFromServer()
    {
        var config = GameManager.Instance?.PendingQuiz ?? QuizConfig.Default();
        _currentIndex = 0;
        _attemptId = null;
        if (IvrApi.Learning != null)
        {
            var result = await IvrApi.Learning.Start(new LearningStartRequest {
                topicId = config.LessonId ?? "", lessonId = config.PersonalLessonId ?? "", diagnostic = config.IsDiagnostic
            });
            if (result.Success && result.Data?.questions?.Count > 0)
            {
                _attemptId = result.Data.id;
                _questions = result.Data.questions;
                return;
            }
        }
        PracticeReason = "Тренировка без сохранения: учебный сервер недоступен. Персональный разбор появится после онлайн-квиза.";
        _questions = LocalQuizQuestions.ForLesson(config.LessonId);
    }

    public async Task<ApiResult<LearningAnswerDto>> Answer(int index)
    {
        if (IsPractice)
            return ApiResult<LearningAnswerDto>.Ok(new LearningAnswerDto {
                correct = index == Current.correctIndex, correctIndex = Current.correctIndex,
                explanation = "Верный ответ: " + Current.answers[Current.correctIndex]
            });
        _pendingAnswer ??= index;
        return await IvrApi.Learning.Answer(_attemptId, Current.id, _pendingAnswer.Value);
    }
    public int SelectedIndex(int requested) => _pendingAnswer ?? requested;
    public bool HasNext() => _currentIndex < _questions.Count - 1;
    public void Next() { _currentIndex++; _pendingAnswer = null; }

    public async Task<bool> Finish(int finalScore)
    {
        if (IsPractice)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            return true;
        }
        var result = await IvrApi.Learning.Finish(_attemptId);
        if (!result.Success) return false;
        GameManager.Instance.AcceptLearningResult(result.Data);
        return true;
    }
}
