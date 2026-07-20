using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    public interface ICommentRepository : IRepository<Comment>
    {
        /// <summary>All comments of a book (flat, with their author loaded); the thread is assembled in the service.</summary>
        Task<IReadOnlyList<Comment>> GetByBookAsync(int bookId, CancellationToken cancellationToken = default);

        /// <summary>A single comment with its author loaded, so the returned DTO carries the user name.</summary>
        Task<Comment?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default);
    }
}
