namespace DigitalBookLibrary.Application.DTOs.Authors
{
    /// <summary>
    /// Payload for creating/updating an author. An author is backed by a Person; a user account is
    /// optional (classic authors have none), so no account fields appear here.
    /// </summary>
    public sealed class SaveAuthorDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
