using DigitalBookLibrary.Application.DTOs.Books;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual entity → DTO mapping for books (no AutoMapper).</summary>
    public static class BookMappings
    {
        /// <summary>
        /// The DB stores an opaque storage key; clients get the API route that streams the cover.
        /// The key itself is never exposed, and files are not reachable by direct URL.
        /// </summary>
        private static string? CoverUrl(Book book)
            => string.IsNullOrWhiteSpace(book.ImageUrl) ? null : $"/api/books/{book.Id}/cover";

        public static BookListDto ToListDto(this Book book) => new()
        {
            Id = book.Id,
            Title = book.Title,
            AuthorId = book.AuthorId,
            AuthorName = book.Author?.Person?.FullName ?? string.Empty,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.Name ?? string.Empty,
            ImageUrl = CoverUrl(book),
            Language = book.Language,
            PublishDate = book.PublishDate,
            IsAvailable = book.IsAvailable,
            IsVisible = book.IsVisible,
            DownloadsCount = book.DownloadsCount,
            ReadsCount = book.ReadsCount
        };

        public static BookDetailsDto ToDetailsDto(this Book book, double averageRating, int ratingCount) => new()
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            AuthorId = book.AuthorId,
            AuthorName = book.Author?.Person?.FullName ?? string.Empty,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.Name ?? string.Empty,
            ImageUrl = CoverUrl(book),
            PublishDate = book.PublishDate,
            Pages = book.Pages,
            Language = book.Language,
            PublisherId = book.PublisherId,
            PublisherName = book.PublisherName,
            FileSizeMb = book.FileSizeMb,
            HasFile = !string.IsNullOrWhiteSpace(book.PdfUrl),
            IsAvailable = book.IsAvailable,
            IsVisible = book.IsVisible,
            DownloadsCount = book.DownloadsCount,
            ReadsCount = book.ReadsCount,
            AverageRating = averageRating,
            RatingCount = ratingCount
        };

        /// <summary>Applies editable metadata onto an entity (shared by create and update).</summary>
        public static void ApplyFrom(this Book book, SaveBookDto dto)
        {
            book.Title = dto.Title.Trim();
            book.AuthorId = dto.AuthorId;
            book.CategoryId = dto.CategoryId;
            book.Description = dto.Description;
            book.PublishDate = dto.PublishDate;
            book.Pages = dto.Pages;
            book.Language = dto.Language;
            book.PublisherName = dto.PublisherName;
        }
    }
}
