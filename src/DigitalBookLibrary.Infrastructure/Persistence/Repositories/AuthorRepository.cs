using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class AuthorRepository : Repository<Author>, IAuthorRepository
    {
        public AuthorRepository(AppDbContext context) : base(context) { }

        public async Task<PagedResult<Author>> GetPagedAsync(
            AuthorQueryOptions options, bool includeHidden, CancellationToken cancellationToken = default)
        {
            var query = Set
                .AsNoTracking()
                .Include(a => a.Person)
                .AsQueryable();

            if (!includeHidden)
            {
                query = query.Where(a => a.IsVisible);
            }

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var term = options.Search.Trim();
                query = query.Where(a =>
                    a.Person != null &&
                    (a.Person.FullName.Contains(term) ||
                     (a.Person.Nationality != null && a.Person.Nationality.Contains(term))));
            }

            query = (options.SortBy?.ToLowerInvariant()) switch
            {
                "date" => options.Desc
                    ? query.OrderByDescending(a => a.DateCreated)
                    : query.OrderBy(a => a.DateCreated),
                _ => options.Desc
                    ? query.OrderByDescending(a => a.Person!.FullName)
                    : query.OrderBy(a => a.Person!.FullName)
            };

            return await query.ToPagedResultAsync(options.PageNumber, options.PageSize, cancellationToken);
        }

        public async Task<Author?> GetWithDetailsAsync(
            int id, bool includeHidden, CancellationToken cancellationToken = default)
        {
            var author = await Set
                .AsNoTracking()
                .Include(a => a.Person)
                .Include(a => a.Books.Where(b => includeHidden || b.IsVisible))
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            return author;
        }
    }
}
