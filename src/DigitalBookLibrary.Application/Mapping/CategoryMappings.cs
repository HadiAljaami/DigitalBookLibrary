using DigitalBookLibrary.Application.DTOs.Categories;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual entity → DTO mapping for categories (no AutoMapper).</summary>
    public static class CategoryMappings
    {
        public static CategoryDto ToDto(this Category category) => new()
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId
        };

        /// <summary>
        /// Builds the category tree in memory from a flat list — categories are few, so one query plus
        /// an in-memory assembly beats recursive DB calls.
        /// </summary>
        public static List<CategoryDto> ToTree(this IEnumerable<Category> categories)
        {
            var nodes = categories.ToDictionary(c => c.Id, c => c.ToDto());
            var roots = new List<CategoryDto>();

            foreach (var node in nodes.Values.OrderBy(n => n.Name))
            {
                if (node.ParentCategoryId is int parentId && nodes.TryGetValue(parentId, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
    }
}
