using IVR.Api;
using IVR.Api.Services;

// Единственная точка входа для всех запросов к бэкенду.
// Использование: IvrApi.Auth.Login(...) / IvrApi.Player.GetProfile()
namespace IVR
{
    public static class IvrApi
    {
        public static AuthService    Auth      { get; private set; }
        public static PlayerService  Player    { get; private set; }
        public static QuestionService Questions { get; private set; }
        public static LearningService Learning { get; private set; }
        public static MarketService  Market    { get; private set; }

        public static void Init(string baseUrl)
        {
            var client = ApiClient.Instance;
            client.Init(baseUrl);
            Auth      = new AuthService(client);
            Player    = new PlayerService(client);
            Questions = new QuestionService(client);
            Market    = new MarketService(client);
            Learning = new LearningService(client);
        }
    }
}
