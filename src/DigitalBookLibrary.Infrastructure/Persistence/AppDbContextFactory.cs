using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    /// <summary>
    /// Design-time factory so <c>dotnet ef</c> can build the context for migrations without the WebAPI
    /// host being wired up. Uses the local dev (LocalDB) connection; runtime reads the string from config.
    /// </summary>
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            const string connectionString =
                "Server=(localdb)\\MSSQLLocalDB;Database=DigitalBookLibrary;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new AppDbContext(options);
        }
    }
}
