using System.Threading.Tasks;
using IVR;
using IVR.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject profilePanel;

    [Header("Login Panel")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Profile Panel")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text interestsText;
    [SerializeField] private TMP_InputField interestsInput;
    [SerializeField] private Button setInterestsButton;
    [SerializeField] private Button addScoreButton;
    [SerializeField] private Button logoutButton;

    [Header("Settings")]
    [SerializeField] private string baseUrl = "http://localhost:8080";

    private PlayerProfile _profile;

    private void Start()
    {
        IvrApi.Init(baseUrl);

        loginButton.onClick.AddListener(() => _ = Login());
        registerButton.onClick.AddListener(() => _ = Register());
        setInterestsButton.onClick.AddListener(() => _ = SetInterests());
        addScoreButton.onClick.AddListener(() => _ = AddScore());
        logoutButton.onClick.AddListener(Logout);

        // если токен сохранён — сразу в профиль
        if (IvrApi.Auth.IsLoggedIn)
            _ = LoadProfile();
        else
            ShowLogin();
    }

    // ─── Авторизация ──────────────────────────────────────

    private async Task Login()
    {
        SetStatus("Вхожу...");
        SetButtonsActive(false);

        var result = await IvrApi.Auth.Login(emailInput.text, passwordInput.text);

        if (result.Success)
            await LoadProfile();
        else
            SetStatus("Ошибка: " + result.Error);

        SetButtonsActive(true);
    }

    private async Task Register()
    {
        SetStatus("Регистрирую...");
        SetButtonsActive(false);

        var result = await IvrApi.Auth.Register(emailInput.text, passwordInput.text);

        if (result.Success)
            await LoadProfile();
        else
            SetStatus("Ошибка: " + result.Error);

        SetButtonsActive(true);
    }

    private void Logout()
    {
        IvrApi.Auth.Logout();
        _profile = null;
        ShowLogin();
    }

    // ─── Профиль ──────────────────────────────────────────

    private async Task LoadProfile()
    {
        var result = await IvrApi.Player.GetProfile();

        if (result.Success)
        {
            _profile = result.Data;
            ShowProfile();
        }
        else
        {
            // токен истёк
            IvrApi.Auth.Logout();
            SetStatus("Сессия истекла, войдите снова");
            ShowLogin();
        }
    }

    private async Task SetInterests()
    {
        if (string.IsNullOrWhiteSpace(interestsInput.text)) return;

        SetButtonsActive(false);
        var result = await IvrApi.Player.SetInterests(interestsInput.text);
        if (result.Success) await LoadProfile();
        SetButtonsActive(true);
    }

    private async Task AddScore()
    {
        if (_profile == null) return;

        SetButtonsActive(false);
        var result = await IvrApi.Player.UpdateProgress(
            _profile.finLiteracyScore + 10,
            _profile.level
        );
        if (result.Success) await LoadProfile();
        SetButtonsActive(true);
    }

    // ─── UI состояния ─────────────────────────────────────

    private void ShowLogin()
    {
        loginPanel.SetActive(true);
        profilePanel.SetActive(false);
    }

    private void ShowProfile()
    {
        loginPanel.SetActive(false);
        profilePanel.SetActive(true);

        nicknameText.text  = $"Игрок: {_profile.nickname}";
        levelText.text     = $"Уровень: {_profile.level}";
        scoreText.text     = $"Очки: {_profile.finLiteracyScore}";
        interestsText.text = $"Интересы: {string.Join(", ", _profile.interests ?? new())}";
    }

    private void SetStatus(string message) => statusText.text = message;

    private void SetButtonsActive(bool active)
    {
        loginButton.interactable    = active;
        registerButton.interactable = active;
    }
}
