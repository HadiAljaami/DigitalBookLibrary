namespace DigitalBookLibrary.Domain.Entities
{

    public class Author
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public Person? Person { get; set; }
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}