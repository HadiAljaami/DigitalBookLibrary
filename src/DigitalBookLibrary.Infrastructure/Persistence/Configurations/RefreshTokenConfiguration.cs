using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(500);
            builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(500);
            builder.Property(t => t.CreatedByIp).HasMaxLength(64);

            builder.Ignore(t => t.IsActive);   // computed, not stored
            builder.HasIndex(t => t.TokenHash);

            builder.HasOne(t => t.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
