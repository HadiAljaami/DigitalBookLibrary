using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
    {
        public void Configure(EntityTypeBuilder<UserAccount> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.Phone).HasMaxLength(30);

            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.Username).IsUnique();

            // Optional 1–1 to Person (a login may or may not be linked to a Person record).
            builder.HasOne(u => u.Person)
                   .WithOne(p => p.UserAccount)
                   .HasForeignKey<UserAccount>(u => u.PersonId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
