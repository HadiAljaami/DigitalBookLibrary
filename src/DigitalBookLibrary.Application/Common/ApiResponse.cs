namespace DigitalBookLibrary.Application.Common
{
    /// <summary>
    /// The single envelope wrapping every API response. Both <see cref="Message"/> and
    /// <see cref="Errors"/> carry stable CODES (never human sentences); the frontend translates them.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        /// <summary>A stable code key (e.g. "SUCCESS", "OPERATION_FAILED") — not a display sentence.</summary>
        public string Message { get; set; } = string.Empty;

        public T? Data { get; set; }

        /// <summary>Error CODES only, for the frontend to translate.</summary>
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string code = ResponseCodes.Success)
            => new() { Success = true, Message = code, Data = data };

        public static ApiResponse<T> Fail(string code, params string[] errorCodes)
            => new() { Success = false, Message = code, Errors = errorCodes.ToList() };
    }

    /// <summary>Terse factories, including responses without a payload.</summary>
    public static class ApiResponse
    {
        public static ApiResponse<T> Ok<T>(T data, string code = ResponseCodes.Success)
            => ApiResponse<T>.Ok(data, code);

        public static ApiResponse<object?> Ok(string code = ResponseCodes.Success)
            => new() { Success = true, Message = code };

        public static ApiResponse<T> Fail<T>(string code, params string[] errorCodes)
            => ApiResponse<T>.Fail(code, errorCodes);

        public static ApiResponse<object?> Fail(string code, params string[] errorCodes)
            => new() { Success = false, Message = code, Errors = errorCodes.ToList() };
    }
}
