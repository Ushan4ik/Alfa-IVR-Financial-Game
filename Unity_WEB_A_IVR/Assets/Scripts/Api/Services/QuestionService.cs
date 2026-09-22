using System.Collections.Generic;
using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class QuestionService
    {
        private readonly ApiClient _client;

        public QuestionService(ApiClient client)
        {
            _client = client;
        }

        // вопросы подобранные под интересы текущего игрока
        public async Task<ApiResult<List<QuestionDto>>> GetForPlayer()
        {
            var result = await _client.Get("/questions");
            if (!result.Success) return ApiResult<List<QuestionDto>>.Fail(result.Error);

            // JsonUtility не умеет десериализовать массив напрямую — оборачиваем
            var json = $"{{\"items\":{result.Data}}}";
            var wrapper = JsonUtility.FromJson<QuestionListWrapper>(json);
            return ApiResult<List<QuestionDto>>.Ok(wrapper.items);
        }

        // вопросы по категории
        public async Task<ApiResult<List<QuestionDto>>> GetByCategory(string category)
        {
            var result = await _client.Get($"/questions/category/{category}");
            if (!result.Success) return ApiResult<List<QuestionDto>>.Fail(result.Error);

            var json = $"{{\"items\":{result.Data}}}";
            var wrapper = JsonUtility.FromJson<QuestionListWrapper>(json);
            return ApiResult<List<QuestionDto>>.Ok(wrapper.items);
        }

        // вопросы конкретного урока
        public async Task<ApiResult<List<QuestionDto>>> GetByLesson(string lessonId)
        {
            var result = await _client.Get($"/questions/lesson/{lessonId}");
            if (!result.Success) return ApiResult<List<QuestionDto>>.Fail(result.Error);

            var json = $"{{\"items\":{result.Data}}}";
            var wrapper = JsonUtility.FromJson<QuestionListWrapper>(json);
            return ApiResult<List<QuestionDto>>.Ok(wrapper.items);
        }
    }
}
