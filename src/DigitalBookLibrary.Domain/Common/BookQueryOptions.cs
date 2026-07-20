namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>Filter/sort/search options for the book catalog listing.</summary>
    public sealed class BookQueryOptions : PaginationParams
    {
        /// <summary>Free-text search over title and description.</summary>
        public string? Search { get; set; }

        public int? AuthorId { get; set; }
        public int? CategoryId { get; set; }
        public string? Language { get; set; }
        public bool? IsAvailable { get; set; }

        /// <summary>title | date | rating | downloads | reads (default: title).</summary>
        public string? SortBy { get; set; }

        public bool Desc { get; set; }
    }
}
