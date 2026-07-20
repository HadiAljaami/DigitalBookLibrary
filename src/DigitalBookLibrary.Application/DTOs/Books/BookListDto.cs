namespace DigitalBookLibrary.Application.DTOs.Books
{
    public sealed class BookListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Language { get; set; }
        public DateOnly? PublishDate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsVisible { get; set; }
        public int DownloadsCount { get; set; }
        public int ReadsCount { get; set; }
    }
}
