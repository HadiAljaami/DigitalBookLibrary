using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    public interface ISavedBookRepository : IRepository<UserSavedBook>
    {
        /// <summary>The user's saved books (most recently saved first), with author/category loaded.</summary>
        Task<PagedResult<Book>> GetPagedByUserAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default);
    }
}
