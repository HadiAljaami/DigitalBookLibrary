namespace DigitalBookLibrary.Application.DTOs.Categories
{
    /// <summary>Payload for creating or updating a category.</summary>
    public sealed class SaveCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
    }
}
