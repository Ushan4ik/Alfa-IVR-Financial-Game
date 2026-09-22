using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class PlayerService
    {
        private readonly ApiClient _client;

        public PlayerService(ApiClient client)
        {
            _client = client;
        }

        public async Task<ApiResult<PlayerProfile>> GetProfile()
        {
            var result = await _client.Get("/player/profile");

            if (!result.Success)
                return ApiResult<PlayerProfile>.Fail(result.Error);

            return ApiResult<PlayerProfile>.Ok(JsonUtility.FromJson<PlayerProfile>(result.Data));
        }

        public async Task<ApiResult<bool>> SetInterests(string text)
        {
            var result = await _client.Post("/player/interests", new SetInterestsRequest { text = text });
            return result.Success ? ApiResult<bool>.Ok(true) : ApiResult<bool>.Fail(result.Error);
        }

        public async Task<ApiResult<bool>> UpdateProgress(int finLiteracyScore, int level)
        {
            var result = await _client.Put("/player/progress", new UpdateProgressRequest
            {
                finLiteracyScore = finLiteracyScore,
                level = level
            });
            return result.Success ? ApiResult<bool>.Ok(true) : ApiResult<bool>.Fail(result.Error);
        }
    }
}
