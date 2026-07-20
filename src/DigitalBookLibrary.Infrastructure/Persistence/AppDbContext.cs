using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Person> Persons => Set<Person>();
        public DbSet<UserAccount> Users => Set<UserAccount>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<Rating> Ratings => Set<Rating>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<UserSavedBook> UserSavedBooks => Set<UserSavedBook>();
        public DbSet<BookReadLog> BookReadLogs => Set<BookReadLog>();
        public DbSet<BookDownloadLog> BookDownloadLogs => Set<BookDownloadLog>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Picks up every IEntityTypeConfiguration<T> in this assembly (Persistence/Configurations).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
