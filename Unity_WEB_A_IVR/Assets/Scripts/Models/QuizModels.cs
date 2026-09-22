using System;
using System.Collections.Generic;

namespace IVR.Models
{
    [Serializable]
    public class QuestionDto
    {
        public string id;
        public string text;
        public List<string> answers;
        public int correctIndex;
        public string category;
        public int level;
    }

    [Serializable]
    public class QuestionListWrapper
    {
        public List<QuestionDto> items;
    }
}
