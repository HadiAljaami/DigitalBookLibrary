namespace DigitalBookLibrary.Application.DTOs.Ratings
{
    public sealed class RatingSummaryDto
    {
        public double Average { get; set; }
        public int Count { get; set; }

        /// <summary>The current user's own rating, when they are signed in and have rated.</summary>
        public int? MyRating { get; set; }
    }
}
