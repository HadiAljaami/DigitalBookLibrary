namespace DigitalBookLibrary.Domain.Entities
{
    public class Person
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public UserAccount? UserAccount { get; set; }
        public Author? Author { get; set; }
    }

}
