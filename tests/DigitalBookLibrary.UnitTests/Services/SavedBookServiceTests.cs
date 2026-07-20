using System.Linq.Expressions;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class SavedBookServiceTests
    {
        private readonly ISavedBookRepository _saved = Substitute.For<ISavedBookRepository>();
        private readonly IBookRepository _books = Substitute.For<IBookRepository>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

        private SavedBookService CreateSut(int? userId = 1)
            => new(_saved, _books, TestDoubles.CurrentUser(userId), _uow);

        // T-9 — saving a book that is already saved is a no-op, so repeated clicks can't duplicate rows.
        [Fact]
        public async Task SaveAsync_AlreadySaved_DoesNothing()
        {
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _saved.ExistsAsync(Arg.Any<Expression<Func<UserSavedBook, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);   // already in the saved list
            var sut = CreateSut();

            await sut.SaveAsync(bookId: 3, CancellationToken.None);

            await _saved.DidNotReceive().AddAsync(Arg.Any<UserSavedBook>(), Arg.Any<CancellationToken>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // The first save inserts exactly one row and commits once.
        [Fact]
        public async Task SaveAsync_NotYetSaved_AddsOnceAndSaves()
        {
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _saved.ExistsAsync(Arg.Any<Expression<Func<UserSavedBook, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);
            var sut = CreateSut(userId: 4);

            await sut.SaveAsync(bookId: 3, CancellationToken.None);

            await _saved.Received(1).AddAsync(
                Arg.Is<UserSavedBook>(s => s.BookId == 3 && s.UserId == 4), Arg.Any<CancellationToken>());
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
