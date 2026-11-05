using System.Collections.Generic;
using System.Threading.Tasks;
using IVR.Models;
using UnityEngine;

namespace IVR.Api.Services
{
    public class LearningService
    {
        private readonly ApiClient _client;
        public LearningService(ApiClient client) { _client = client; }
        private static ApiResult<T> Parse<T>(ApiResult<string> result) => result.Success
            ? ApiResult<T>.Ok(JsonUtility.FromJson<T>(result.Data)) : ApiResult<T>.Fail(result.Error);
        public async Task<ApiResult<LearningAttemptDto>> Start(LearningStartRequest request) =>
            Parse<LearningAttemptDto>(await _client.Post("/learning/attempts", request));
        public async Task<ApiResult<LearningAnswerDto>> Answer(string id, string questionId, int selectedIndex) =>
            Parse<LearningAnswerDto>(await _client.Post("/learning/attempts/" + id + "/answers",
                new LearningAnswerRequest { questionId = questionId, selectedIndex = selectedIndex }));
        public async Task<ApiResult<LearningFinishDto>> Finish(string id) =>
            Parse<LearningFinishDto>(await _client.Post("/learning/attempts/" + id + "/finish", new LearningEmptyRequest()));
        public async Task<ApiResult<LearningProfileDto>> Profile() =>
            Parse<LearningProfileDto>(await _client.Get("/learning/profile"));
        public async Task<ApiResult<PersonalLessonDto>> TopicStory(string topicId) =>
            Parse<PersonalLessonDto>(await _client.Post(
                "/learning/topics/" + topicId + "/story", new LearningEmptyRequest()));
        public async Task<ApiResult<List<PersonalLessonDto>>> Lessons()
        {
            var result = await _client.Get("/learning/lessons");
            if (!result.Success) return ApiResult<List<PersonalLessonDto>>.Fail(result.Error);
            return ApiResult<List<PersonalLessonDto>>.Ok(JsonUtility.FromJson<PersonalLessonList>("{\"items\":" + result.Data + "}").items);
        }
    }
}
