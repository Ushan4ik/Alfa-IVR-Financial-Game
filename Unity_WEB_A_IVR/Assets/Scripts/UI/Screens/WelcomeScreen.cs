using UnityEngine.UIElements;

namespace IVR.UI
{
    public class WelcomeScreen : BaseScreen
    {
        public WelcomeScreen(VisualTreeAsset asset, VisualElement container)
            : base(asset, container) { }

        protected override void RegisterCallbacks()
        {
            Root.Q<Button>("start-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Register);
            Root.Q<Button>("login-btn").clicked += () => ScreenManager.Instance.Show(AppScreen.Login);
        }
    }
}
