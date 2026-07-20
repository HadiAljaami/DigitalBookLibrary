using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
            builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).HasMaxLength(50);
            builder.Property(a => a.IpAddress).HasMaxLength(64);
            // OldValues / NewValues are JSON snapshots — leave as nvarchar(max).

            builder.HasIndex(a => new { a.EntityName, a.CreatedAt });
            // No FK navigation: UserId is a soft reference (nullable, kept even if the user is removed).
        }
    }
}
