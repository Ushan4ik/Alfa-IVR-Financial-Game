namespace IVR.Game
{
    public class QuizConfig
    {
        public string LessonId  { get; }
        public string Category  { get; }
        public string PersonalLessonId { get; private set; }
        public bool IsDiagnostic { get; private set; }
        public int    Count     { get; }

        private QuizConfig(string lessonId, string category, int count)
        {
            LessonId = lessonId;
            Category = category;
            Count    = count;
        }

        public static QuizConfig Diagnostic() => new QuizConfig(null, null, 4) { IsDiagnostic = true };
        public static QuizConfig ForPersonalLesson(string id, string topic) =>
            new QuizConfig(topic, null, 3) { PersonalLessonId = id };

        // вопросы по конкретному уроку
        public static QuizConfig ForLesson(string lessonId, int count = 5) =>
            new QuizConfig(lessonId, null, count);

        // вопросы по категории
        public static QuizConfig ForCategory(string category, int count = 5) =>
            new QuizConfig(null, category, count);

        // вопросы под интересы игрока (по умолчанию)
        public static QuizConfig Default(int count = 5) =>
            new QuizConfig(null, null, count);
    }
}
