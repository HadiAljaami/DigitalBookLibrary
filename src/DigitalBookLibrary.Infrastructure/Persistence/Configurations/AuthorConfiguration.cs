using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.ToTable("Authors");
            builder.HasKey(a => a.Id);

            // An author is always backed by a Person (which may or may not have a UserAccount).
            builder.HasOne(a => a.Person)
                   .WithOne(p => p.Author)
                   .HasForeignKey<Author>(a => a.PersonId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
