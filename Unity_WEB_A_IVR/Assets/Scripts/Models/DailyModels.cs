using System;
using System.Collections.Generic;

namespace IVR.Models
{
    [Serializable]
    public class DailyQuestDto
    {
        public string id;
        public string title;
        public int progress;
        public int target;
        public bool completed;
        public bool available;
    }

    [Serializable]
    public class DailyProgressDto
    {
        public string date;
        public int currentStreak;
        public int bestStreak;
        public int completedCount;
        public int totalCount;
        public List<DailyQuestDto> quests;
        public int coins;
        public double marketCash;
        public List<StreakRewardDto> streakRewards;
    }

    [Serializable]
    public class StreakRewardDto
    {
        public int days;
        public int coins;
        public int marketCash;
        public bool unlocked;
        public bool claimed;
    }
}
