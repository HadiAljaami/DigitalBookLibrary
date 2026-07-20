namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Comment errors. See docs/06-Error-Codes.md §7.</summary>
    public static class CommentErrors
    {
        public static readonly Error NotFound =
            new("COMMENT_NOT_FOUND", "No comment exists with the supplied identifier.");

        public static readonly Error TextRequired =
            new("COMMENT_TEXT_REQUIRED", "The comment text is required.");

        public static readonly Error AccessDenied =
            new("COMMENT_ACCESS_DENIED", "The current user is neither the comment owner nor an admin.");

        public static readonly Error ParentInvalid =
            new("COMMENT_PARENT_INVALID", "The parent comment is missing or belongs to another book.");
    }
}
