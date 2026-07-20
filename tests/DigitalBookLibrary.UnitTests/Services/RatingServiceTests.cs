using System.Linq.Expressions;
using DigitalBookLibrary.Application.DTOs.Ratings;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class RatingServiceTests
    {
        private readonly IRepository<Rating> _ratings = Substitute.For<IRepository<Rating>>();
        private readonly IBookRepository _books = Substitute.For<IBookRepository>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

        private RatingService CreateSut(int? userId = 1)
            => new(_ratings, _books, TestDoubles.CurrentUser(userId), _uow);

        // T-1 — rating must be 1–5.
        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-3)]
        public async Task RateAsync_ValueOutOfRange_ThrowsAndDoesNotSave(int value)
        {
            var sut = CreateSut();

            var act = () => sut.RateAsync(bookId: 1, new RateBookDto { Value = value }, CancellationToken.None);

            var ex = await Should.ThrowAsync<ValidationAppException>(act);
            ex.Errors.ShouldContain(e => e.Code == "RATING_OUT_OF_RANGE");
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // The boundary values 1 and 5 are valid and must NOT trip the range guard.
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task RateAsync_BoundaryValue_IsAccepted(int value)
        {
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _books.GetRatingSummaryAsync(1, Arg.Any<CancellationToken>()).Returns((value, 1));
            var sut = CreateSut();

            var act = () => sut.RateAsync(bookId: 1, new RateBookDto { Value = value }, CancellationToken.None);

            await Should.NotThrowAsync(act);
        }

        // T-2 — re-rating the same book updates the existing row instead of inserting a second one.
        [Fact]
        public async Task RateAsync_UserAlreadyRated_UpdatesInsteadOfInserting()
        {
            const int bookId = 7;
            var existing = new Rating { Id = 42, BookId = bookId, UserId = 1, Value = 2 };

            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _ratings.FirstOrDefaultAsync(Arg.Any<Expression<Func<Rating, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(existing);
            _books.GetRatingSummaryAsync(bookId, Arg.Any<CancellationToken>()).Returns((5d, 1));
            var sut = CreateSut();

            await sut.RateAsync(bookId, new RateBookDto { Value = 5 }, CancellationToken.None);

            existing.Value.ShouldBe(5);
            _ratings.Received(1).Update(existing);
            await _ratings.DidNotReceive().AddAsync(Arg.Any<Rating>(), Arg.Any<CancellationToken>());
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
