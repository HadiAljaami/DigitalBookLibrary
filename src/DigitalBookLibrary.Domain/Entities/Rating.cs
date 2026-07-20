namespace DigitalBookLibrary.Domain.Entities
{
    public class Rating
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public int Value { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public Book? Book { get; set; }
        public UserAccount? User { get; set; }
    }
}


