using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Authors;
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
    /// Author management. An author is always backed by a <see cref="Person"/>; a user account is
    /// optional (classic authors have none), so creating an author never creates a login.
    /// </summary>
    public class AuthorService
    {
        private readonly IAuthorRepository _authors;
        private readonly IRepository<Person> _persons;
        private readonly IRepository<Book> _books;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IValidator<SaveAuthorDto> _validator;

        public AuthorService(
            IAuthorRepository authors,
            IRepository<Person> persons,
            IRepository<Book> books,
            ICurrentUser currentUser,
            IUnitOfWork uow,
            IValidator<SaveAuthorDto> validator)
        {
            _authors = authors;
            _persons = persons;
            _books = books;
            _currentUser = currentUser;
            _uow = uow;
            _validator = validator;
        }

        public async Task<PagedResult<AuthorListDto>> GetPagedAsync(
            AuthorQueryOptions options, CancellationToken cancellationToken = default)
        {
            var page = await _authors.GetPagedAsync(options, IsAdmin, cancellationToken);

            return new PagedResult<AuthorListDto>(
                page.Items.Select(a => a.ToListDto()).ToList(),
                page.TotalCount,
                page.PageNumber,
                page.PageSize);
        }

        public async Task<AuthorDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var author = await _authors.GetWithDetailsAsync(id, IsAdmin, cancellationToken)
                ?? throw new NotFoundException(AuthorErrors.NotFound);

            // Hidden authors are invisible to everyone but admins.
            if (!author.IsVisible && !IsAdmin)
            {
                throw new NotFoundException(AuthorErrors.NotFound);
            }

            return author.ToDetailsDto();
        }

        public async Task<AuthorDetailsDto> CreateAsync(
            SaveAuthorDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var person = new Person();
            person.ApplyFrom(dto);

            var author = new Author
            {
                Person = person,
                IsVisible = dto.IsVisible
            };

            await _authors.AddAsync(author, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return author.ToDetailsDto();
        }

        public async Task<AuthorDetailsDto> UpdateAsync(
            int id, SaveAuthorDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var author = await _authors.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                ?? throw new NotFoundException(AuthorErrors.NotFound);

            var person = await _persons.GetByIdAsync(author.PersonId, cancellationToken)
                ?? throw new NotFoundException(AuthorErrors.NotFound);

            person.ApplyFrom(dto);
            author.IsVisible = dto.IsVisible;

            _persons.Update(person);
            _authors.Update(author);
            await _uow.SaveChangesAsync(cancellationToken);

            author.Person = person;
            return author.ToDetailsDto();
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var author = await _authors.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(AuthorErrors.NotFound);

            if (await _books.ExistsAsync(b => b.AuthorId == id, cancellationToken))
            {
                throw new ConflictException(AuthorErrors.HasBooks);
            }

            _authors.Remove(author);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        private bool IsAdmin => _currentUser.IsInRole(Roles.Admin);
    }
}
