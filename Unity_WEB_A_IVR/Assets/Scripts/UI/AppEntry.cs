using IVR;
using IVR.Api;
using IVR.Game;
using IVR.UI;
using UnityEngine;

// Точка входа в приложение. Прикрепи к тому же GameObject что и ScreenManager.
public class AppEntry : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:8080";

    private async void Start()
    {
        IvrApi.Init(baseUrl);

        if (IvrApi.Auth.IsLoggedIn)
        {
            var profile = await IvrApi.Player.GetProfile();
            if (profile.Success)
            {
                GameManager.Instance.SetProfile(profile.Data);
                ScreenManager.Instance.Show(AppScreen.Hub);
            }
            else
            {
                IvrApi.Auth.Logout();
                ScreenManager.Instance.Show(AppScreen.Welcome);
            }
        }
        else
        {
            ScreenManager.Instance.Show(AppScreen.Welcome);
        }
    }
}
