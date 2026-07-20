using DigitalBookLibrary.Application.Common;
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
    /// <summary>
    /// Reading and downloading books, plus the member's "My Library" history.
    /// The counter and the log row are committed together in one unit of work, so a book can never
    /// be counted without a matching log entry (or vice-versa).
    /// </summary>
    public class BookActivityService
    {
        private readonly IBookRepository _books;
        private readonly IRepository<BookReadLog> _readLogs;
        private readonly IRepository<BookDownloadLog> _downloadLogs;
        private readonly IBookActivityRepository _activity;
        private readonly IFileStorageService _files;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public BookActivityService(
            IBookRepository books,
            IRepository<BookReadLog> readLogs,
            IRepository<BookDownloadLog> downloadLogs,
            IBookActivityRepository activity,
            IFileStorageService files,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _books = books;
            _readLogs = readLogs;
            _downloadLogs = downloadLogs;
            _activity = activity;
            _files = files;
            _currentUser = currentUser;
            _uow = uow;
        }

        /// <summary>Reads a book online: logs the read, bumps the counter, then streams the PDF.</summary>
        public async Task<(Stream Content, string ContentType, string FileName)> ReadAsync(
            int bookId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var book = await LoadReadableBookAsync(bookId, cancellationToken);

            book.IncrementReads();
            await _readLogs.AddAsync(new BookReadLog { BookId = book.Id, UserId = userId }, cancellationToken);
            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);

            return await OpenBookFileAsync(book, cancellationToken);
        }

        /// <summary>
        /// Downloads a book. <see cref="Book.IncrementDownloads"/> enforces the availability rule in the
        /// domain itself, so an unavailable book can never be counted or served.
        /// </summary>
        public async Task<(Stream Content, string ContentType, string FileName)> DownloadAsync(
            int bookId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var book = await LoadReadableBookAsync(bookId, cancellationToken);

            if (!book.IsAvailable)
            {
                throw new ConflictException(BookErrors.NotAvailable);
            }

            book.IncrementDownloads();
            await _downloadLogs.AddAsync(
                new BookDownloadLog { BookId = book.Id, UserId = userId }, cancellationToken);
            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);

            return await OpenBookFileAsync(book, cancellationToken);
        }

        /// <summary>My Library → Read tab.</summary>
        public async Task<PagedResult<BookListDto>> GetReadHistoryAsync(
            PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var page = await _activity.GetReadHistoryAsync(userId, pagination, cancellationToken);
            return ToDtoPage(page);
        }

        /// <summary>My Library → Downloaded tab.</summary>
        public async Task<PagedResult<BookListDto>> GetDownloadHistoryAsync(
            PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();
            var page = await _activity.GetDownloadHistoryAsync(userId, pagination, cancellationToken);
            return ToDtoPage(page);
        }

        private static PagedResult<BookListDto> ToDtoPage(PagedResult<Book> page)
            => new(page.Items.Select(b => b.ToListDto()).ToList(), page.TotalCount, page.PageNumber, page.PageSize);

        private async Task<Book> LoadReadableBookAsync(int bookId, CancellationToken cancellationToken)
        {
            var book = await _books.GetByIdAsync(bookId, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            if (!book.IsVisible && !_currentUser.IsInRole(Roles.Admin))
            {
                throw new NotFoundException(BookErrors.NotFound);
            }

            if (string.IsNullOrWhiteSpace(book.PdfUrl))
            {
                throw new NotFoundException(BookErrors.FileMissing);
            }

            return book;
        }

        private async Task<(Stream Content, string ContentType, string FileName)> OpenBookFileAsync(
            Book book, CancellationToken cancellationToken)
        {
            var stream = await _files.OpenReadAsync(book.PdfUrl!, cancellationToken);
            var fileName = $"{book.Title}.pdf";
            return (stream, FileValidation.ResolveContentType(book.PdfUrl!), fileName);
        }
    }
}
