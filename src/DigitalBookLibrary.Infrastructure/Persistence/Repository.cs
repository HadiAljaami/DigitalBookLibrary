using System.Linq.Expressions;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    /// <summary>
    /// Generic EF Core repository. Reads use <c>AsNoTracking</c>; persistence is deferred to
    /// <see cref="IUnitOfWork"/> (this type never calls SaveChanges). Entity-specific repositories
    /// derive from this to add Include/projection/paging queries.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<T> Set;

        public Repository(AppDbContext context)
        {
            Context = context;
            Set = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await Set.FindAsync(new object?[] { id }, cancellationToken);

        public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
            => await Set.AsNoTracking().ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

        public async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await Set.FirstOrDefaultAsync(predicate, cancellationToken);

        public async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await Set.AnyAsync(predicate, cancellationToken);

        public async Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
            => predicate is null
                ? await Set.CountAsync(cancellationToken)
                : await Set.CountAsync(predicate, cancellationToken);

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
            => await Set.AddAsync(entity, cancellationToken);

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            => await Set.AddRangeAsync(entities, cancellationToken);

        public void Update(T entity) => Set.Update(entity);

        public void Remove(T entity) => Set.Remove(entity);
    }
}
