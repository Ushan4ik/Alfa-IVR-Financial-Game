namespace IVR.Game
{
    public class MiniGameResult
    {
        public string GameId      { get; }  // "quiz", "budget", etc.
        public int    Score       { get; }  // очки за эту игру
        public bool   Passed      { get; }  // прошёл или нет

        public MiniGameResult(string gameId, int score, bool passed)
        {
            GameId  = gameId;
            Score   = score;
            Passed  = passed;
        }
    }
}
