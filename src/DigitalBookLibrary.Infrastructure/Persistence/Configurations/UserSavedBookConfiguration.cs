using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class UserSavedBookConfiguration : IEntityTypeConfiguration<UserSavedBook>
    {
        public void Configure(EntityTypeBuilder<UserSavedBook> builder)
        {
            builder.ToTable("UserSavedBooks");
            builder.HasKey(s => s.Id);

            // A book can be saved once per user.
            builder.HasIndex(s => new { s.UserId, s.BookId }).IsUnique();

            builder.HasOne(s => s.Book)
                   .WithMany()
                   .HasForeignKey(s => s.BookId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.User)
                   .WithMany()
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
