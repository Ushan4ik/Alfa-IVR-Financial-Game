using System.Threading.Tasks;
using IVR;
using IVR.Models;
using UnityEngine;

// Минимальный UI для проверки бэкенда.
// Прикрепи к любому GameObject, укажи baseUrl в инспекторе.
public class TestUI : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:8080";

    private string _email    = "test@test.com";
    private string _password = "12345678";
    private string _interests = "люблю экономику и математику";

    private string _status = "Не авторизован";
    private PlayerProfile _profile;
    private bool _loading;

    private void Start()
    {
        IvrApi.Init(baseUrl);
    }

    private void OnGUI()
    {
        var skin = GUI.skin;
        skin.button.fontSize  = 16;
        skin.label.fontSize   = 16;
        skin.textField.fontSize = 16;

        GUILayout.BeginArea(new Rect(20, 20, 420, Screen.height - 40));

        GUILayout.Label("=== IVR Backend Test ===");
        GUILayout.Space(10);

        // --- Авторизация ---
        GUILayout.Label("Email:");
        _email = GUILayout.TextField(_email, GUILayout.Height(30));
        GUILayout.Label("Пароль:");
        _password = GUILayout.PasswordField(_password, '*', GUILayout.Height(30));
        GUILayout.Space(5);

        GUI.enabled = !_loading;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Войти", GUILayout.Height(36)))    _ = Login();
        if (GUILayout.Button("Регистрация", GUILayout.Height(36))) _ = Register();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label($"Статус: {_status}");

        // --- Профиль (только после логина) ---
        if (_profile != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("─── Профиль ───");
            GUILayout.Label($"Никнейм:  {_profile.nickname}");
            GUILayout.Label($"Уровень:  {_profile.level}");
            GUILayout.Label($"Очки:     {_profile.finLiteracyScore}");
            GUILayout.Label($"Интересы: {string.Join(", ", _profile.interests ?? new())}");

            GUILayout.Space(10);
            GUILayout.Label("─── Интересы ───");
            _interests = GUILayout.TextField(_interests, GUILayout.Height(30));
            if (GUILayout.Button("Определить интересы через ML", GUILayout.Height(36)))
                _ = SetInterests();

            GUILayout.Space(10);
            GUILayout.Label("─── Прогресс ───");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 очков", GUILayout.Height(36)))
                _ = AddScore(10);
            if (GUILayout.Button("+1 уровень", GUILayout.Height(36)))
                _ = AddLevel();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("Обновить профиль", GUILayout.Height(36)))
                _ = RefreshProfile();
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private async Task Login()
    {
        SetLoading("Вхожу...");
        var result = await IvrApi.Auth.Login(_email, _password);
        if (result.Success) { SetStatus("Вошёл: " + result.Data.playerId[..8]); await RefreshProfile(); }
        else SetStatus("Ошибка: " + result.Error);
    }

    private async Task Register()
    {
        SetLoading("Регистрирую...");
        var result = await IvrApi.Auth.Register(_email, _password);
        if (result.Success) { SetStatus("Зарегистрирован!"); await RefreshProfile(); }
        else SetStatus("Ошибка: " + result.Error);
    }

    private async Task RefreshProfile()
    {
        SetLoading("Загружаю профиль...");
        var result = await IvrApi.Player.GetProfile();
        if (result.Success) { _profile = result.Data; SetStatus("OK"); }
        else SetStatus("Ошибка профиля: " + result.Error);
    }

    private async Task SetInterests()
    {
        SetLoading("Отправляю в ML...");
        var result = await IvrApi.Player.SetInterests(_interests);
        if (result.Success) { SetStatus("Интересы сохранены"); await RefreshProfile(); }
        else SetStatus("Ошибка: " + result.Error);
    }

    private async Task AddScore(int amount)
    {
        if (_profile == null) return;
        SetLoading("Сохраняю...");
        var result = await IvrApi.Player.UpdateProgress(_profile.finLiteracyScore + amount, _profile.level);
        if (result.Success) await RefreshProfile();
        else SetStatus("Ошибка: " + result.Error);
    }

    private async Task AddLevel()
    {
        if (_profile == null) return;
        SetLoading("Сохраняю...");
        var result = await IvrApi.Player.UpdateProgress(_profile.finLiteracyScore, _profile.level + 1);
        if (result.Success) await RefreshProfile();
        else SetStatus("Ошибка: " + result.Error);
    }

    private void SetLoading(string msg) { _status = msg; _loading = true; }
    private void SetStatus(string msg)  { _status = msg; _loading = false; }
}
