using UnityEngine;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public enum AppScreen { Welcome, Login, Register, Onboarding, Hub }

    // Единственная точка управления экранами.
    // Добавь новый экран: 1) значение в enum 2) поле + Show метод ниже.
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset welcomeScreenAsset;
        [SerializeField] private VisualTreeAsset loginScreenAsset;
        [SerializeField] private VisualTreeAsset registerScreenAsset;
        [SerializeField] private VisualTreeAsset onboardingScreenAsset;
        [SerializeField] private VisualTreeAsset hubScreenAsset;

        private VisualElement _root;
        private AppScreen _current;

        private WelcomeScreen _welcome;
        private LoginScreen    _login;
        private RegisterScreen _register;
        private OnboardingScreen _onboarding;
        private HubScreen _hub;
        private bool _ready;

        private void Awake()
        {
            Instance = this;
            _root = uiDocument.rootVisualElement;

            // Ссылка в открытой сцене могла устареть после переименования UXML.
            // Resources позволяет найти меню и в редакторе, и в WebGL-сборке.
            hubScreenAsset = Resources.Load<VisualTreeAsset>("UI/HubScreen") ?? hubScreenAsset;
            if (welcomeScreenAsset == null || loginScreenAsset == null ||
                registerScreenAsset == null || onboardingScreenAsset == null || hubScreenAsset == null)
            {
                const string message = "Не удалось загрузить экраны игры. Проверь UI-ассеты в сцене Main.";
                Debug.LogError(message);
                var error = new Label(message);
                error.style.flexGrow = 1;
                error.style.unityTextAlign = TextAnchor.MiddleCenter;
                error.style.color = Color.red;
                _root.Add(error);
                return;
            }

            _welcome    = new WelcomeScreen(welcomeScreenAsset, _root);
            _login      = new LoginScreen(loginScreenAsset, _root);
            _register   = new RegisterScreen(registerScreenAsset, _root);
            _onboarding = new OnboardingScreen(onboardingScreenAsset, _root);
            _hub        = new HubScreen(hubScreenAsset, _root);

            _welcome.InitializeCallbacks();
            _login.InitializeCallbacks();
            _register.InitializeCallbacks();
            _onboarding.InitializeCallbacks();
            _hub.InitializeCallbacks();
            _ready = true;
        }

        private void Start()
        {
            Show(AppScreen.Welcome);
        }

        public void Show(AppScreen screen)
        {
            if (!_ready) return;
            _current = screen;

            _welcome.Hide();
            _login.Hide();
            _register.Hide();
            _onboarding.Hide();
            _hub.Hide();

            switch (screen)
            {
                case AppScreen.Welcome:    _welcome.Show();    break;
                case AppScreen.Login:      _login.Show();      break;
                case AppScreen.Register:   _register.Show();   break;
                case AppScreen.Onboarding: _onboarding.Show(); break;
                case AppScreen.Hub:        _hub.Show();        break;
            }
        }

        public AppScreen Current => _current;

        public void ShowInterests()
        {
            if (!_ready) return;
            Show(AppScreen.Onboarding);
            _onboarding.GoToInterests(true);
        }
    }
}
