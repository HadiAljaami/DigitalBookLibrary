using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>Author queries needing the linked Person and/or their books.</summary>
    public interface IAuthorRepository : IRepository<Author>
    {
        Task<PagedResult<Author>> GetPagedAsync(
            AuthorQueryOptions options, bool includeHidden, CancellationToken cancellationToken = default);

        /// <summary>An author with Person and their visible books loaded.</summary>
        Task<Author?> GetWithDetailsAsync(
            int id, bool includeHidden, CancellationToken cancellationToken = default);
    }
}
