using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// Read/download history queries for the member's "My Library". A book read many times appears
    /// once, ordered by its most recent activity.
    /// </summary>
    public interface IBookActivityRepository
    {
        Task<PagedResult<Book>> GetReadHistoryAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default);

        Task<PagedResult<Book>> GetDownloadHistoryAsync(
            int userId, PaginationParams pagination, CancellationToken cancellationToken = default);
    }
}
