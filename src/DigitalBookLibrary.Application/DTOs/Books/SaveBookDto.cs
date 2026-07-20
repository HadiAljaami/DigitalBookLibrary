namespace DigitalBookLibrary.Application.DTOs.Books
{
    /// <summary>
    /// Payload for creating/updating a book's metadata. File references (PdfUrl/ImageUrl) are set via
    /// the upload endpoints, and the counters/visibility flags are controlled by their own endpoints.
    /// </summary>
    public sealed class SaveBookDto
    {
        public string Title { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public DateOnly? PublishDate { get; set; }
        public int? Pages { get; set; }
        public string? Language { get; set; }
        public string? PublisherName { get; set; }
    }
}
