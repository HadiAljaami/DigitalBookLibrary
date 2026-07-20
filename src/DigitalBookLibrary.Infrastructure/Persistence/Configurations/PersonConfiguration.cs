using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Bio).HasMaxLength(2000);
            builder.Property(p => p.Nationality).HasMaxLength(100);
            builder.Property(p => p.City).HasMaxLength(100);
            builder.Property(p => p.Country).HasMaxLength(100);
            builder.Property(p => p.ImageUrl).HasMaxLength(500);

            // 1–1 Person↔UserAccount and Person↔Author are configured from the dependent sides.
        }
    }
}
