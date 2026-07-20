namespace DigitalBookLibrary.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }

        public Category? ParentCategory { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}