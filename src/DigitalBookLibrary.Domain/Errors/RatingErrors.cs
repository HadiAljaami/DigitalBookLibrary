namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Rating errors. See docs/06-Error-Codes.md §6.</summary>
    public static class RatingErrors
    {
        public static readonly Error OutOfRange =
            new("RATING_OUT_OF_RANGE", "The rating value must be between 1 and 5.");

        public static readonly Error NotFound =
            new("RATING_NOT_FOUND", "This user has no rating for this book.");
    }
}
