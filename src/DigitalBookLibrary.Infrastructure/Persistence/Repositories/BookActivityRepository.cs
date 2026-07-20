using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class BookActivityRepository : IBookActivityRepository
    {
        private readonly AppDbContext _context;

        public BookActivityRepository(AppDbContext context) => _context = context;

        /// <remarks>
        /// Rooted on Books so Include stays valid (EF rejects Include after Select), and the log
        /// DbSet is used directly in the sub-queries so EF can translate them to SQL EXISTS/MAX.
        /// A book read many times appears once, ordered by its most recent read.
        /// </remarks>
        public Task<PagedResult<Book>> GetReadHistoryAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var query = BooksWithDetails()
                .Where(b => b.IsVisible &&
                            _context.BookReadLogs.Any(l => l.UserId == userId && l.BookId == b.Id))
                .OrderByDescending(b => _context.BookReadLogs
                    .Where(l => l.UserId == userId && l.BookId == b.Id)
                    .Max(l => l.DateRead));

            return query.ToPagedResultAsync(pagination.PageNumber, pagination.PageSize, cancellationToken);
        }

        public Task<PagedResult<Book>> GetDownloadHistoryAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var query = BooksWithDetails()
                .Where(b => b.IsVisible &&
                            _context.BookDownloadLogs.Any(l => l.UserId == userId && l.BookId == b.Id))
                .OrderByDescending(b => _context.BookDownloadLogs
                    .Where(l => l.UserId == userId && l.BookId == b.Id)
                    .Max(l => l.DateDownloaded));

            return query.ToPagedResultAsync(pagination.PageNumber, pagination.PageSize, cancellationToken);
        }

        private IQueryable<Book> BooksWithDetails()
            => _context.Books
                .AsNoTracking()
                .Include(b => b.Author).ThenInclude(a => a!.Person)
                .Include(b => b.Category);
    }
}
