using System.Threading.Tasks;
using IVR.Api;
using IVR.Models;
using IVR.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IVR.Game
{
    // Живёт между всеми сценами.
    // Хранит профиль игрока и принимает результаты миниигр.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerProfile  Profile      { get; private set; }
        public MiniGameResult LastResult   { get; private set; }
        public string         LastSaveError { get; private set; }
        public QuizConfig     PendingQuiz  { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetProfile(PlayerProfile profile)
        {
            Profile = profile;
        }

        public string LastLearningLessonId { get; private set; }

        public void AcceptLearningResult(LearningFinishDto result)
        {
            LastResult = new MiniGameResult(PendingQuiz?.LessonId ?? "quiz", result.awardedScore, result.passed);
            LastSaveError = null;
            LastLearningLessonId = result.lessonId;
            if (result.passed && Profile != null && !string.IsNullOrEmpty(PendingQuiz?.LessonId))
                LessonCatalog.MarkCompleted(Profile.id, PendingQuiz.LessonId);
            BackToMain();
        }

        public void ClearLastResult()
        {
            LastResult = null;
            LastLearningLessonId = null;
            LastSaveError = null;
        }

        // Запустить квиз с нужной конфигурацией
        public void StartQuiz(QuizConfig config = null)
        {
            PendingQuiz = config ?? QuizConfig.Default();
            SceneManager.LoadScene("Quiz");
        }

        public void LoadGame(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void BackToMain()
        {
            SceneManager.LoadScene("Main");
        }
    }
}
