using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DigitalBookLibrary.Infrastructure.Persistence.Repositories
{
    public sealed class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Comment>> GetByBookAsync(
            int bookId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.DateCreated)
                .ToListAsync(cancellationToken);

        public async Task<Comment?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
