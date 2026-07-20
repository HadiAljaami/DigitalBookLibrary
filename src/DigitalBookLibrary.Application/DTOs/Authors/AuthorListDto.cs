namespace DigitalBookLibrary.Application.DTOs.Authors
{
    public sealed class AuthorListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Nationality { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsVisible { get; set; }

        /// <summary>True when the author's person also has a login account (i.e. can publish).</summary>
        public bool HasAccount { get; set; }
    }
}
