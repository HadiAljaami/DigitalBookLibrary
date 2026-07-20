namespace DigitalBookLibrary.Domain.Entities
{
    public class UserSavedBook
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime DateSaved { get; set; } = DateTime.UtcNow;

        public UserAccount? User { get; set; }
        public Book? Book { get; set; }
    }

}

