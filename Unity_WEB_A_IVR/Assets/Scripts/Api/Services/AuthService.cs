using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class AuthService
    {
        private readonly ApiClient _client;

        public AuthService(ApiClient client)
        {
            _client = client;
        }

        public async Task<ApiResult<AuthResponse>> Register(string email, string password, string nickname = "")
        {
            var result = await _client.Post("/auth/register", new RegisterRequest
            {
                email = email,
                password = password,
                nickname = nickname
            });

            if (!result.Success)
                return ApiResult<AuthResponse>.Fail(result.Error);

            var response = JsonUtility.FromJson<AuthResponse>(result.Data);
            _client.SetToken(response.token);
            return ApiResult<AuthResponse>.Ok(response);
        }

        public async Task<ApiResult<AuthResponse>> Login(string email, string password)
        {
            var result = await _client.Post("/auth/login", new LoginRequest
            {
                email = email,
                password = password
            });

            if (!result.Success)
                return ApiResult<AuthResponse>.Fail(result.Error);

            var response = JsonUtility.FromJson<AuthResponse>(result.Data);
            _client.SetToken(response.token);
            return ApiResult<AuthResponse>.Ok(response);
        }

        public bool IsLoggedIn => _client.IsLoggedIn;

        public void Logout()
        {
            _client.ClearToken();
        }
    }
}
