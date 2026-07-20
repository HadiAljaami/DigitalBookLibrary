namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Saved-book errors. See docs/06-Error-Codes.md §8.</summary>
    public static class SavedBookErrors
    {
        public static readonly Error NotFound =
            new("SAVED_BOOK_NOT_FOUND", "The book is not in the user's saved list.");
    }
}
