using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// Book queries that need eager loading, aggregation or paging. Implemented in Infrastructure with
    /// EF Core, returning materialized results so the Application layer never touches EF.
    /// </summary>
    public interface IBookRepository : IRepository<Book>
    {
        /// <param name="includeHidden">Admins see hidden/unavailable books; guests never do.</param>
        Task<PagedResult<Book>> GetPagedAsync(
            BookQueryOptions options, bool includeHidden, CancellationToken cancellationToken = default);

        /// <summary>A single book with Author (+Person) and Category loaded.</summary>
        Task<Book?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Average rating and rating count for a book (0 / 0 when it has no ratings).</summary>
        Task<(double Average, int Count)> GetRatingSummaryAsync(
            int bookId, CancellationToken cancellationToken = default);
    }
}
