using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Books;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Mapping;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>
    /// Book catalog: listing (paged/filtered/sorted/searched), details, CRUD and the
    /// visibility/availability toggles. Guests only ever see visible books; admins see everything.
    /// </summary>
    public class BookService
    {
        private const string PdfFolder = "books";
        private const string CoverFolder = "covers";

        private readonly IBookRepository _books;
        private readonly IRepository<Author> _authors;
        private readonly IRepository<Category> _categories;
        private readonly IFileStorageService _files;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IValidator<SaveBookDto> _validator;

        public BookService(
            IBookRepository books,
            IRepository<Author> authors,
            IRepository<Category> categories,
            IFileStorageService files,
            ICurrentUser currentUser,
            IUnitOfWork uow,
            IValidator<SaveBookDto> validator)
        {
            _books = books;
            _authors = authors;
            _categories = categories;
            _files = files;
            _currentUser = currentUser;
            _uow = uow;
            _validator = validator;
        }

        public async Task<PagedResult<BookListDto>> GetPagedAsync(
            BookQueryOptions options, CancellationToken cancellationToken = default)
        {
            var page = await _books.GetPagedAsync(options, IsAdmin, cancellationToken);

            return new PagedResult<BookListDto>(
                page.Items.Select(b => b.ToListDto()).ToList(),
                page.TotalCount,
                page.PageNumber,
                page.PageSize);
        }

        public async Task<BookDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var book = await _books.GetWithDetailsAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            // Hidden books are indistinguishable from missing ones for non-admins.
            if (!book.IsVisible && !IsAdmin)
            {
                throw new NotFoundException(BookErrors.NotFound);
            }

            var (average, count) = await _books.GetRatingSummaryAsync(id, cancellationToken);
            return book.ToDetailsDto(average, count);
        }

        public async Task<BookDetailsDto> CreateAsync(SaveBookDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);
            await EnsureAuthorAndCategoryExistAsync(dto, cancellationToken);

            var book = new Book();
            book.ApplyFrom(dto);

            // A non-admin creator owns the book they upload; an admin may add on behalf of the library.
            book.PublisherId = IsAdmin ? null : _currentUser.RequireUserId();

            await _books.AddAsync(book, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(book.Id, cancellationToken);
        }

        public async Task<BookDetailsDto> UpdateAsync(
            int id, SaveBookDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            EnsureCanManage(book);
            await EnsureAuthorAndCategoryExistAsync(dto, cancellationToken);

            book.ApplyFrom(dto);
            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(book.Id, cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            EnsureCanManage(book);

            _books.Remove(book);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        public async Task SetVisibilityAsync(int id, bool isVisible, CancellationToken cancellationToken = default)
        {
            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            book.SetVisibility(isVisible);   // encapsulated domain state
            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        public async Task SetAvailabilityAsync(int id, bool isAvailable, CancellationToken cancellationToken = default)
        {
            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            book.SetAvailability(isAvailable);
            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Uploads/replaces the book's PDF and records its size. Returns nothing sensitive.</summary>
        public async Task UploadPdfAsync(int id, FileUploadRequest file, CancellationToken cancellationToken = default)
        {
            var book = await LoadManageableAsync(id, cancellationToken);
            await FileValidation.ValidatePdfAsync(file, cancellationToken);

            var previousKey = book.PdfUrl;
            book.PdfUrl = await _files.SaveAsync(file, PdfFolder, cancellationToken);
            book.FileSizeMb = Math.Round(file.Length / (1024m * 1024m), 2);

            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);

            await DeleteIfReplacedAsync(previousKey, book.PdfUrl, cancellationToken);
        }

        /// <summary>Uploads/replaces the cover image.</summary>
        public async Task UploadCoverAsync(int id, FileUploadRequest file, CancellationToken cancellationToken = default)
        {
            var book = await LoadManageableAsync(id, cancellationToken);
            await FileValidation.ValidateImageAsync(file, cancellationToken);

            var previousKey = book.ImageUrl;
            book.ImageUrl = await _files.SaveAsync(file, CoverFolder, cancellationToken);

            _books.Update(book);
            await _uow.SaveChangesAsync(cancellationToken);

            await DeleteIfReplacedAsync(previousKey, book.ImageUrl, cancellationToken);
        }

        /// <summary>Opens the cover image for streaming (public, but hidden books stay hidden).</summary>
        public async Task<(Stream Content, string ContentType)> OpenCoverAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            if (!book.IsVisible && !IsAdmin)
            {
                throw new NotFoundException(BookErrors.NotFound);
            }

            if (string.IsNullOrWhiteSpace(book.ImageUrl))
            {
                throw new NotFoundException(BookErrors.FileMissing);
            }

            var stream = await _files.OpenReadAsync(book.ImageUrl, cancellationToken);
            return (stream, FileValidation.ResolveContentType(book.ImageUrl));
        }

        private async Task<Book> LoadManageableAsync(int id, CancellationToken cancellationToken)
        {
            var book = await _books.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(BookErrors.NotFound);

            EnsureCanManage(book);
            return book;
        }

        /// <summary>Removes the previous file only after the new one is safely committed.</summary>
        private async Task DeleteIfReplacedAsync(string? previousKey, string? newKey, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(previousKey) && previousKey != newKey)
            {
                await _files.DeleteAsync(previousKey, cancellationToken);
            }
        }

        private bool IsAdmin => _currentUser.IsInRole(Roles.Admin);

        /// <summary>Admins manage any book; everyone else only the books they published.</summary>
        private void EnsureCanManage(Book book)
        {
            if (IsAdmin)
            {
                return;
            }

            if (book.PublisherId is null || book.PublisherId != _currentUser.RequireUserId())
            {
                throw new ForbiddenException(BookErrors.AccessDenied);
            }
        }

        private async Task EnsureAuthorAndCategoryExistAsync(SaveBookDto dto, CancellationToken cancellationToken)
        {
            if (!await _authors.ExistsAsync(a => a.Id == dto.AuthorId, cancellationToken))
            {
                throw new ValidationAppException(BookErrors.AuthorRequired);
            }

            if (!await _categories.ExistsAsync(c => c.Id == dto.CategoryId, cancellationToken))
            {
                throw new ValidationAppException(BookErrors.CategoryRequired);
            }
        }
    }
}
