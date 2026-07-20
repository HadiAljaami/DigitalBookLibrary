using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    public sealed class SeedOptions
    {
        public const string SectionName = "Seed";

        public string AdminUsername { get; set; } = "admin";
        public string AdminEmail { get; set; } = "admin@digitalbooklibrary.local";

        /// <summary>Dev-only default. Override via user-secrets/env in any real deployment.</summary>
        public string AdminPassword { get; set; } = "Admin#12345";
    }

    /// <summary>
    /// Ensures the baseline data exists: the Admin/Member roles (also seeded via HasData) and one
    /// admin account. Idempotent — safe to run on every startup.
    /// </summary>
    public sealed class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _hasher;
        private readonly SeedOptions _options;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            AppDbContext context,
            IPasswordHasher hasher,
            IOptions<SeedOptions> options,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _hasher = hasher;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            await EnsureRolesAsync(cancellationToken);
            await EnsureAdminAsync(cancellationToken);
        }

        private async Task EnsureRolesAsync(CancellationToken cancellationToken)
        {
            foreach (var roleName in new[] { Roles.Admin, Roles.Member })
            {
                if (!await _context.Roles.AnyAsync(r => r.Name == roleName, cancellationToken))
                {
                    _context.Roles.Add(new Role { Name = roleName });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureAdminAsync(CancellationToken cancellationToken)
        {
            var adminRole = await _context.Roles.FirstAsync(r => r.Name == Roles.Admin, cancellationToken);

            var admin = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == _options.AdminEmail, cancellationToken);

            if (admin is null)
            {
                admin = new UserAccount
                {
                    Username = _options.AdminUsername,
                    Email = _options.AdminEmail,
                    PasswordHash = _hasher.Hash(_options.AdminPassword)
                };
                admin.UserRoles.Add(new UserRole { Role = adminRole });

                _context.Users.Add(admin);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Seeded admin account {Email}", _options.AdminEmail);
                return;
            }

            // Account exists but lost its role (e.g. manual DB edits) — restore it.
            if (admin.UserRoles.All(ur => ur.RoleId != adminRole.Id))
            {
                admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Restored Admin role for {Email}", _options.AdminEmail);
            }
        }
    }
}
