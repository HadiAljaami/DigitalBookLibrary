using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : Repository<UserAccount>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<UserAccount?> GetByEmailOrUsernameAsync(
            string identifier, CancellationToken cancellationToken = default)
            => await Set
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier, cancellationToken);

        public async Task<UserAccount?> GetByIdWithRolesAsync(int id, CancellationToken cancellationToken = default)
            => await Set
                .Include(u => u.Person)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
