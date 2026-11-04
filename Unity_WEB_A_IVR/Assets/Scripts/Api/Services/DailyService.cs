using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class DailyService
    {
        private readonly ApiClient _client;

        public DailyService(ApiClient client)
        {
            _client = client;
        }

        public async Task<ApiResult<DailyProgressDto>> GetProgress()
        {
            var result = await _client.Get("/daily");
            if (!result.Success)
                return ApiResult<DailyProgressDto>.Fail(result.Error);
            return ApiResult<DailyProgressDto>.Ok(JsonUtility.FromJson<DailyProgressDto>(result.Data));
        }

        // Мини-игра вызывает этот метод после первого игрового действия.
        public async Task<ApiResult<DailyProgressDto>> CompleteMiniGame()
        {
            var result = await _client.Post("/daily/minigame-completed", new { });
            if (!result.Success)
                return ApiResult<DailyProgressDto>.Fail(result.Error);
            return ApiResult<DailyProgressDto>.Ok(JsonUtility.FromJson<DailyProgressDto>(result.Data));
        }
    }
}
