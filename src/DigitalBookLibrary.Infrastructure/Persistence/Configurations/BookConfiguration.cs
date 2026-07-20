using DigitalBookLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalBookLibrary.Infrastructure.Persistence.Configurations
{
    public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("Books");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Title).IsRequired().HasMaxLength(300);
            builder.Property(b => b.Description).HasMaxLength(4000);
            builder.Property(b => b.ImageUrl).HasMaxLength(500);
            builder.Property(b => b.PdfUrl).HasMaxLength(500);
            builder.Property(b => b.Language).HasMaxLength(50);
            builder.Property(b => b.PublisherName).HasMaxLength(200);
            builder.Property(b => b.FileSizeMb).HasPrecision(9, 2);

            builder.HasIndex(b => b.Title);

            // Encapsulated state (private setters) is mapped by EF via the backing property.
            builder.Property(b => b.IsAvailable);
            builder.Property(b => b.IsVisible);
            builder.Property(b => b.DownloadsCount);
            builder.Property(b => b.ReadsCount);

            builder.HasOne(b => b.Author)
                   .WithMany(a => a.Books)
                   .HasForeignKey(b => b.AuthorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Category)
                   .WithMany(c => c.Books)
                   .HasForeignKey(b => b.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Optional publisher (the account that uploaded/owns the book).
            builder.HasOne(b => b.Publisher)
                   .WithMany(u => u.PublishedBooks)
                   .HasForeignKey(b => b.PublisherId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
