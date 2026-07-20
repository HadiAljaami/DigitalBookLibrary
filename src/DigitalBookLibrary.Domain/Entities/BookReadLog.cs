namespace DigitalBookLibrary.Domain.Entities
{
    public class BookReadLog
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public DateTime DateRead { get; set; } = DateTime.UtcNow;

        public Book? Book { get; set; }
        public UserAccount? User { get; set; }
    }
}


