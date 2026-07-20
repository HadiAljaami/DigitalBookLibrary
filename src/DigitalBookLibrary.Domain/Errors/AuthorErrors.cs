namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Author errors. See docs/06-Error-Codes.md §4.</summary>
    public static class AuthorErrors
    {
        public static readonly Error NotFound =
            new("AUTHOR_NOT_FOUND", "No author exists with the supplied identifier.");

        public static readonly Error NameRequired =
            new("AUTHOR_NAME_REQUIRED", "The linked person's full name is required.");

        public static readonly Error HasBooks =
            new("AUTHOR_HAS_BOOKS", "Cannot delete an author that still has books.");
    }
}
