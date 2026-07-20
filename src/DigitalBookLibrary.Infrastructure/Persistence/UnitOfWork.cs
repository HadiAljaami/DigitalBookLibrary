using DigitalBookLibrary.Domain.Interfaces;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    /// <summary>Thin wrapper over the scoped <see cref="AppDbContext"/> — commits one transaction per request.</summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context) => _context = context;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
