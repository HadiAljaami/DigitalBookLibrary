namespace DigitalBookLibrary.Application.DTOs.Categories
{
    /// <summary>A category node; <see cref="Children"/> is populated when returned as a tree.</summary>
    public sealed class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public List<CategoryDto> Children { get; set; } = new();
    }
}
