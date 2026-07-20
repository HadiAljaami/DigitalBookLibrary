using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class BookDownloadLogConfiguration : IEntityTypeConfiguration<BookDownloadLog>
    {
        public void Configure(EntityTypeBuilder<BookDownloadLog> builder)
        {
            builder.ToTable("BookDownloadLogs");
            builder.HasKey(l => l.Id);

            builder.HasIndex(l => new { l.UserId, l.DateDownloaded });

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
