using IVR.Api;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public class RegisterScreen : BaseScreen
    {
        private TextField _nickname;
        private TextField _email;
        private TextField _password;
        private TextField _repeatPassword;
        private Button _registerButton;

        public RegisterScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            _nickname = Root.Q<TextField>("nickname-field");
            _email = Root.Q<TextField>("email-field");
            _password = Root.Q<TextField>("password-field");
            _repeatPassword = Root.Q<TextField>("repeat-password-field");
            _registerButton = Root.Q<Button>("register-btn");

            _registerButton.clicked += () => _ = Register();
            Root.Q<Button>("login-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Login);
            Root.Q<Button>("back-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Welcome);
        }

        private async System.Threading.Tasks.Task Register()
        {
            var email = _email.value?.Trim() ?? "";
            var nickname = _nickname.value?.Trim() ?? "";
            var password = _password.value ?? "";

            if (nickname.Length == 0 || !email.Contains("@") || email.StartsWith("@") || email.EndsWith("@"))
            {
                SetStatus("status-text", "Введи имя и корректную почту", isError: true);
                return;
            }
            if (password.Length < 8)
            {
                SetStatus("status-text", "Пароль должен содержать хотя бы 8 символов", isError: true);
                return;
            }
            if (password != _repeatPassword.value)
            {
                SetStatus("status-text", "Пароли не совпадают", isError: true);
                return;
            }

            _registerButton.SetEnabled(false);
            SetStatus("status-text", "Создаём аккаунт...");
            try
            {
                var result = await IvrApi.Auth.Register(email, password, nickname);
                if (result.Success)
                    ScreenManager.Instance.Show(AppScreen.Onboarding);
                else
                    SetStatus("status-text", result.Error, isError: true);
            }
            finally
            {
                _registerButton.SetEnabled(true);
            }
        }
    }
}
