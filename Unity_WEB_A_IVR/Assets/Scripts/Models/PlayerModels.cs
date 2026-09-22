using System;
using System.Collections.Generic;

namespace IVR.Models
{
    [Serializable]
    public class PlayerProfile
    {
        public string id;
        public string email;
        public string nickname;
        public int level;
        public int finLiteracyScore;
        public List<string> interests;
    }

    [Serializable]
    public class SetInterestsRequest
    {
        public string text;
    }

    [Serializable]
    public class UpdateProgressRequest
    {
        public int finLiteracyScore;
        public int level;
    }
}
