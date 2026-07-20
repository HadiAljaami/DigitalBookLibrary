using DigitalBookLibrary.Application.DTOs.Books;

namespace DigitalBookLibrary.Application.DTOs.Authors
{
    public sealed class AuthorDetailsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsVisible { get; set; }
        public bool HasAccount { get; set; }
        public List<BookListDto> Books { get; set; } = new();
    }
}
