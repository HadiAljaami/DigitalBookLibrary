using DigitalBookLibrary.Application.DTOs.Books;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Mapping;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>The member's saved/favourite books. Saving is idempotent.</summary>
    public class SavedBookService
    {
        private readonly ISavedBookRepository _saved;
        private readonly IBookRepository _books;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public SavedBookService(
            ISavedBookRepository saved,
            IBookRepository books,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _saved = saved;
            _books = books;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<PagedResult<BookListDto>> GetPagedAsync(
            PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var page = await _saved.GetPagedByUserAsync(userId, pagination, cancellationToken);

            return new PagedResult<BookListDto>(
                page.Items.Select(b => b.ToListDto()).ToList(),
                page.TotalCount,
                page.PageNumber,
                page.PageSize);
        }

        /// <summary>Saving an already-saved book is a no-op, so repeated clicks can't create duplicates.</summary>
        public async Task SaveAsync(int bookId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();

            if (!await _books.ExistsAsync(b => b.Id == bookId && b.IsVisible, cancellationToken))
            {
                throw new NotFoundException(BookErrors.NotFound);
            }

            if (await _saved.ExistsAsync(s => s.BookId == bookId && s.UserId == userId, cancellationToken))
            {
                return;
            }

            await _saved.AddAsync(new UserSavedBook { BookId = bookId, UserId = userId }, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        public async Task UnsaveAsync(int bookId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();

            var entry = await _saved.FirstOrDefaultAsync(
                s => s.BookId == bookId && s.UserId == userId, cancellationToken)
                ?? throw new NotFoundException(SavedBookErrors.NotFound);

            _saved.Remove(entry);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
