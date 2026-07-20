namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>Filter/sort/search options for the author listing.</summary>
    public sealed class AuthorQueryOptions : PaginationParams
    {
        /// <summary>Free-text search over the linked person's full name and nationality.</summary>
        public string? Search { get; set; }

        /// <summary>name | date (default: name).</summary>
        public string? SortBy { get; set; }

        public bool Desc { get; set; }
    }
}
