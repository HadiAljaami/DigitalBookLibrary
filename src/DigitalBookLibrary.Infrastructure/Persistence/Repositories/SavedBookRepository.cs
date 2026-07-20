using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class SavedBookRepository : Repository<UserSavedBook>, ISavedBookRepository
    {
        public SavedBookRepository(AppDbContext context) : base(context) { }

        public async Task<PagedResult<Book>> GetPagedByUserAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var saved = Set.AsNoTracking().Where(s => s.UserId == userId);

            // Rooted on Books so Include stays valid — EF rejects Include applied after a Select.
            // (UserId, BookId) is unique, so Max(DateSaved) is simply this user's save time.
            var query = Context.Books
                .AsNoTracking()
                .Include(b => b.Author).ThenInclude(a => a!.Person)
                .Include(b => b.Category)
                .Where(b => b.IsVisible && saved.Any(s => s.BookId == b.Id))
                .OrderByDescending(b => saved.Where(s => s.BookId == b.Id).Max(s => s.DateSaved));

            return await query.ToPagedResultAsync(pagination.PageNumber, pagination.PageSize, cancellationToken);
        }
    }
}
