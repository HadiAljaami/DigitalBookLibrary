namespace DigitalBookLibrary.Domain.Entities
{

    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? PdfUrl { get; set; }
        public DateOnly? PublishDate { get; set; }
        public int? Pages { get; set; }
        public string? Language { get; set; }
        public int? PublisherId { get; set; }
        public string? PublisherName { get; set; }
        public decimal? FileSizeMb { get; set; }
        public bool IsAvailable { get; private set; } = true;
        public bool IsVisible { get; private set; } = true;
        public int DownloadsCount { get; private set; }
        public int ReadsCount { get; private set; }

        /// <summary>
        /// When the book was added to the library — distinct from <see cref="PublishDate"/>, which is
        /// when the book itself was published. Needed by the dashboard's recent-activity feed.
        /// </summary>
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public Author? Author { get; set; }
        public Category? Category { get; set; }
        public UserAccount? Publisher { get; set; }
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public void SetVisibility(bool isVisible) => IsVisible = isVisible;

        public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;

        public void IncrementDownloads()
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Book is not available.");
            }

            DownloadsCount++;
        }

        public void IncrementReads()
        {
            ReadsCount++;
        }
    }
}