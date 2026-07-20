namespace DigitalBookLibrary.Domain.Entities
{
    public class BookDownloadLog
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public DateTime DateDownloaded { get; set; } = DateTime.UtcNow;

        public Book? Book { get; set; }
        public UserAccount? User { get; set; }
    }

}

