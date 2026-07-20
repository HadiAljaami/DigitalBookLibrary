namespace DigitalBookLibrary.Application.Common
{
    /// <summary>
    /// Stable success/message code keys placed in <see cref="ApiResponse{T}.Message"/>.
    /// These are codes, not display text; the frontend maps them (see docs/06-Error-Codes.md §1–2).
    /// </summary>
    public static class ResponseCodes
    {
        // General
        public const string Success = "SUCCESS";
        public const string Created = "CREATED";
        public const string Updated = "UPDATED";
        public const string Deleted = "DELETED";
        public const string OperationFailed = "OPERATION_FAILED";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";

        // Auth
        public const string Registered = "REGISTERED";
        public const string LoggedIn = "LOGGED_IN";
        public const string LoggedOut = "LOGGED_OUT";
        public const string TokenRefreshed = "TOKEN_REFRESHED";
    }
}
