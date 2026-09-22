using IVR.Api;
using UnityEngine.UIElements;

namespace IVR.UI
{
    public class LoginScreen : BaseScreen
    {
        private TextField _email;
        private TextField _password;

        public LoginScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            _email    = Root.Q<TextField>("email-field");
            _password = Root.Q<TextField>("password-field");

            Root.Q<Button>("login-btn").clicked    += () => _ = Login();
            Root.Q<Button>("register-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Register);
            Root.Q<Button>("back-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Welcome);
        }

        private async System.Threading.Tasks.Task Login()
        {
            var button = Root.Q<Button>("login-btn");
            if (string.IsNullOrWhiteSpace(_email.value) || string.IsNullOrEmpty(_password.value))
            {
                SetStatus("status-text", "Введи почту и пароль", isError: true);
                return;
            }

            button.SetEnabled(false);
            SetStatus("status-text", "Входим...");
            var result = await IvrApi.Auth.Login(_email.value.Trim(), _password.value);
            button.SetEnabled(true);

            if (result.Success)
                ScreenManager.Instance.Show(AppScreen.Hub);
            else
                SetStatus("status-text", result.Error, isError: true);
        }

    }
}
