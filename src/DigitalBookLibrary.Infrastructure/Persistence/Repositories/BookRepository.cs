using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class BookRepository : Repository<Book>, IBookRepository
    {
        public BookRepository(AppDbContext context) : base(context) { }

        public async Task<PagedResult<Book>> GetPagedAsync(
            BookQueryOptions options, bool includeHidden, CancellationToken cancellationToken = default)
        {
            var query = Set
                .AsNoTracking()
                .Include(b => b.Author).ThenInclude(a => a!.Person)
                .Include(b => b.Category)
                .AsQueryable();

            if (!includeHidden)
            {
                query = query.Where(b => b.IsVisible);
            }

            // Filtering
            if (options.AuthorId is not null)
            {
                query = query.Where(b => b.AuthorId == options.AuthorId);
            }

            if (options.CategoryId is not null)
            {
                query = query.Where(b => b.CategoryId == options.CategoryId);
            }

            if (!string.IsNullOrWhiteSpace(options.Language))
            {
                query = query.Where(b => b.Language == options.Language);
            }

            if (options.IsAvailable is not null)
            {
                query = query.Where(b => b.IsAvailable == options.IsAvailable);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var term = options.Search.Trim();
                query = query.Where(b =>
                    b.Title.Contains(term) ||
                    (b.Description != null && b.Description.Contains(term)));
            }

            // Sorting
            query = (options.SortBy?.ToLowerInvariant()) switch
            {
                "date" => options.Desc
                    ? query.OrderByDescending(b => b.PublishDate)
                    : query.OrderBy(b => b.PublishDate),
                "downloads" => options.Desc
                    ? query.OrderByDescending(b => b.DownloadsCount)
                    : query.OrderBy(b => b.DownloadsCount),
                "reads" => options.Desc
                    ? query.OrderByDescending(b => b.ReadsCount)
                    : query.OrderBy(b => b.ReadsCount),
                "rating" => options.Desc
                    ? query.OrderByDescending(b => b.Ratings.Any() ? b.Ratings.Average(r => r.Value) : 0)
                    : query.OrderBy(b => b.Ratings.Any() ? b.Ratings.Average(r => r.Value) : 0),
                _ => options.Desc
                    ? query.OrderByDescending(b => b.Title)
                    : query.OrderBy(b => b.Title)
            };

            return await query.ToPagedResultAsync(options.PageNumber, options.PageSize, cancellationToken);
        }

        public async Task<Book?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(b => b.Author).ThenInclude(a => a!.Person)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        public async Task<(double Average, int Count)> GetRatingSummaryAsync(
            int bookId, CancellationToken cancellationToken = default)
        {
            var summary = await Context.Ratings
                .AsNoTracking()
                .Where(r => r.BookId == bookId)
                .GroupBy(r => r.BookId)
                .Select(g => new { Average = g.Average(r => (double)r.Value), Count = g.Count() })
                .FirstOrDefaultAsync(cancellationToken);

            return summary is null ? (0d, 0) : (Math.Round(summary.Average, 2), summary.Count);
        }
    }
}
