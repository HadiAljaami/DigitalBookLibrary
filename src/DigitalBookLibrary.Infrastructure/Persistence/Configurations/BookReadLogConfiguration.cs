using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class BookReadLogConfiguration : IEntityTypeConfiguration<BookReadLog>
    {
        public void Configure(EntityTypeBuilder<BookReadLog> builder)
        {
            builder.ToTable("BookReadLogs");
            builder.HasKey(l => l.Id);

            builder.HasIndex(l => new { l.UserId, l.DateRead });

            builder.HasOne(l => l.Book)
                   .WithMany()
                   .HasForeignKey(l => l.BookId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.User)
                   .WithMany()
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
