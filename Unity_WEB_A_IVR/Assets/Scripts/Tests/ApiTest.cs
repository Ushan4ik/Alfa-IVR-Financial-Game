using System.Threading.Tasks;
using IVR;
using UnityEngine;

// Временный скрипт для проверки связи Unity <-> Backend.
// Прикрепи к любому GameObject на сцене, укажи baseUrl и email/password.
// Удали после того как убедишься что всё работает.
public class ApiTest : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string baseUrl = "http://localhost:8080";

    [Header("Test Credentials")]
    [SerializeField] private string email = "test@test.com";
    [SerializeField] private string password = "12345678";

    private async void Start()
    {
        IvrApi.Init(baseUrl);
        await RunTests();
    }

    private async Task RunTests()
    {
        Debug.Log("=== API TEST START ===");

        await TestRegister();
        await TestLogin();
        await TestGetProfile();
        await TestSetInterests();
        await TestUpdateProgress();

        Debug.Log("=== API TEST END ===");
    }

    private async Task TestRegister()
    {
        var result = await IvrApi.Auth.Register(email, password);
        if (result.Success)
            Debug.Log($"[Register] OK — playerId: {result.Data.playerId}");
        else
            Debug.LogWarning($"[Register] SKIP (уже зарегистрирован): {result.Error}");
    }

    private async Task TestLogin()
    {
        var result = await IvrApi.Auth.Login(email, password);
        if (result.Success)
            Debug.Log($"[Login] OK — token получен");
        else
            Debug.LogError($"[Login] FAIL: {result.Error}");
    }

    private async Task TestGetProfile()
    {
        var result = await IvrApi.Player.GetProfile();
        if (result.Success)
            Debug.Log($"[Profile] OK — nickname: {result.Data.nickname}, level: {result.Data.level}");
        else
            Debug.LogError($"[Profile] FAIL: {result.Error}");
    }

    private async Task TestSetInterests()
    {
        var result = await IvrApi.Player.SetInterests("люблю экономику и математику");
        if (result.Success)
            Debug.Log("[Interests] OK");
        else
            Debug.LogError($"[Interests] FAIL: {result.Error}");
    }

    private async Task TestUpdateProgress()
    {
        var result = await IvrApi.Player.UpdateProgress(finLiteracyScore: 100, level: 2);
        if (result.Success)
            Debug.Log("[Progress] OK");
        else
            Debug.LogError($"[Progress] FAIL: {result.Error}");
    }
}
