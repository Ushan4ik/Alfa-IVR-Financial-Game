using System;
using System.Collections.Generic;

namespace IVR.Models
{
    [Serializable] public class LearningStartRequest { public string topicId; public string lessonId; public bool diagnostic; }
    [Serializable] public class LearningAttemptDto { public string id; public List<QuestionDto> questions; public bool diagnostic; }
    [Serializable] public class LearningAnswerRequest { public string questionId; public int selectedIndex; }
    [Serializable] public class LearningAnswerDto { public bool correct; public int correctIndex; public string explanation; }
    [Serializable] public class LearningFinishDto {
        public int score; public int awardedScore; public int correctCount; public int total;
        public bool passed; public string lessonId;
    }
    [Serializable] public class LessonCharacterDto { public string name; public int age; public string personality; }
    [Serializable] public class LessonTermDto {
        public string term; public string text; public string definition; public string example;
    }
    [Serializable] public class PersonalLessonDto {
        public string id; public string topicId; public string status; public bool completed;
        public string title; public string explanation; public string example; public List<string> cheatSheet;
        public string story; public string takeaway; public LessonCharacterDto character;
        public List<LessonTermDto> terms; public string source; public string modelVersion;
    }
    [Serializable] public class PersonalLessonList { public List<PersonalLessonDto> items; }
    [Serializable] public class LearningProfileDto { public bool diagnosticCompleted; public List<TopicProgressDto> topics; }
    [Serializable] public class TopicProgressDto { public string topicId; public string title; public int correct; public int total; }
    [Serializable] public class LearningEmptyRequest { }
}
