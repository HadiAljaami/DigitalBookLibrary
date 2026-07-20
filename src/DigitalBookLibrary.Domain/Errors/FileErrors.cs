namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>File upload errors. See docs/06-Error-Codes.md §9.</summary>
    public static class FileErrors
    {
        public static readonly Error Required =
            new("FILE_REQUIRED", "No file was supplied.");

        public static readonly Error TypeNotAllowed =
            new("FILE_TYPE_NOT_ALLOWED", "The file extension or content-type is not permitted.");

        public static readonly Error TooLarge =
            new("FILE_TOO_LARGE", "The file exceeds the configured maximum size.");

        public static readonly Error Corrupted =
            new("FILE_CORRUPTED", "The file failed the magic-number/header integrity check.");

        public static readonly Error SaveFailed =
            new("FILE_SAVE_FAILED", "Writing the file to storage failed; see logs for details.");
    }
}
