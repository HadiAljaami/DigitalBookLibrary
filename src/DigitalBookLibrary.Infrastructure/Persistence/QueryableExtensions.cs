using DigitalBookLibrary.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    /// <summary>
    /// Shared query helpers used by repositories. Keeps EF Core paging logic in one place so
    /// entity-specific repositories don't repeat Count/Skip/Take.
    /// </summary>
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
        }
    }
}
