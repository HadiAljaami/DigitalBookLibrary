using System.Linq.Expressions;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// Generic data-access abstraction. Implemented in Infrastructure over EF Core.
    /// It exposes only framework-agnostic operations — no <c>IQueryable</c> leaks out — so the
    /// Application layer never depends on EF Core. Predicate helpers use <see cref="Expression"/>
    /// (System.Linq.Expressions), which is EF-free. Complex reads (Include/projection/paging) live
    /// in entity-specific repositories that extend this one.
    /// Persisting is the job of <see cref="IUnitOfWork"/>; repositories never call SaveChanges.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        void Update(T entity);

        void Remove(T entity);
    }
}
