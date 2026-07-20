namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Book errors. See docs/06-Error-Codes.md §3.</summary>
    public static class BookErrors
    {
        public static readonly Error NotFound =
            new("BOOK_NOT_FOUND", "No book exists with the supplied identifier.");

        public static readonly Error NotAvailable =
            new("BOOK_NOT_AVAILABLE", "The book is marked unavailable and cannot be downloaded.");

        public static readonly Error NotVisible =
            new("BOOK_NOT_VISIBLE", "The book is hidden from the public catalog.");

        public static readonly Error FileMissing =
            new("BOOK_FILE_MISSING", "The book has no PDF file associated with it.");

        public static readonly Error TitleRequired =
            new("BOOK_TITLE_REQUIRED", "The book title is required.");

        public static readonly Error AuthorRequired =
            new("BOOK_AUTHOR_REQUIRED", "A valid AuthorId is required.");

        public static readonly Error CategoryRequired =
            new("BOOK_CATEGORY_REQUIRED", "A valid CategoryId is required.");

        public static readonly Error AccessDenied =
            new("BOOK_ACCESS_DENIED", "The current user is neither the book owner nor an admin.");
    }
}
