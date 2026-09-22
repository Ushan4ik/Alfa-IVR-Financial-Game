namespace IVR.Api
{
    public class ApiResult<T>
    {
        public bool Success { get; private set; }
        public T Data { get; private set; }
        public string Error { get; private set; }

        private ApiResult(bool success, T data, string error)
        {
            Success = success;
            Data = data;
            Error = error;
        }

        public static ApiResult<T> Ok(T data) => new ApiResult<T>(true, data, null);
        public static ApiResult<T> Fail(string error) => new ApiResult<T>(false, default, error);
    }
}
