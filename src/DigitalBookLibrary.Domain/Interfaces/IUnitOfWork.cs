namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// Commits all changes tracked by the shared DbContext as one logical transaction.
    /// Implemented in Infrastructure as a thin wrapper injected with the EF Core DbContext.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
