using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace IVR.Api
{
    public class ApiClient
    {
        private static ApiClient _instance;
        public static ApiClient Instance => _instance ??= new ApiClient();

        private const string TokenKey = "ivr_token";

        private string _baseUrl;
        private string _token;

        public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

        public void Init(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _token = PlayerPrefs.GetString(TokenKey, null); // восстанавливаем токен при старте
        }

        public void SetToken(string token)
        {
            _token = token;
            PlayerPrefs.SetString(TokenKey, token); // сохраняем на диск
            PlayerPrefs.Save();
        }

        public void ClearToken()
        {
            _token = null;
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }

        public async Task<ApiResult<string>> Get(string endpoint)
        {
            using var request = UnityWebRequest.Get(_baseUrl + endpoint);
            AddAuthHeader(request);
            return await Send(request);
        }

        public async Task<ApiResult<string>> Post(string endpoint, object body)
        {
            var json = JsonUtility.ToJson(body);
            using var request = new UnityWebRequest(_baseUrl + endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AddAuthHeader(request);
            return await Send(request);
        }

        public async Task<ApiResult<string>> Put(string endpoint, object body)
        {
            var json = JsonUtility.ToJson(body);
            using var request = new UnityWebRequest(_baseUrl + endpoint, "PUT");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            AddAuthHeader(request);
            return await Send(request);
        }

        private void AddAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_token))
                request.SetRequestHeader("Authorization", "Bearer " + _token);
        }

        private static async Task<ApiResult<string>> Send(UnityWebRequest request)
        {
            request.timeout = 20;
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
                return ApiResult<string>.Ok(request.downloadHandler.text);

            return ApiResult<string>.Fail($"{request.responseCode}: {request.downloadHandler.text}");
        }
    }
}
