using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// User-specific queries that need eager-loaded navigations (roles, person). Implemented in
    /// Infrastructure with EF Core includes, keeping those queries out of the Application layer.
    /// </summary>
    public interface IUserRepository : IRepository<UserAccount>
    {
        Task<UserAccount?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default);

        Task<UserAccount?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default);
    }
}
