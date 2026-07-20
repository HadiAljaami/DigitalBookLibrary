namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Generic, cross-cutting errors. See docs/06-Error-Codes.md §1.</summary>
    public static class CommonErrors
    {
        public static readonly Error Validation =
            new("VALIDATION_FAILED", "One or more input validation rules failed.");

        public static readonly Error Unauthorized =
            new("UNAUTHORIZED", "Authentication is required or the provided token is missing/invalid.");

        public static readonly Error Forbidden =
            new("FORBIDDEN", "The user is authenticated but not allowed to perform this action.");

        public static readonly Error NotFound =
            new("NOT_FOUND", "The requested resource was not found.");

        public static readonly Error Internal =
            new("INTERNAL_SERVER_ERROR", "An unhandled server error occurred; see logs for details.");
    }
}
