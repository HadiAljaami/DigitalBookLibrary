using DigitalBookLibrary.Application.DTOs.Ratings;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>Book ratings: one per user per book (re-rating updates), plus the public average.</summary>
    public class RatingService
    {
        private const int MinValue = 1;
        private const int MaxValue = 5;

        private readonly IRepository<Rating> _ratings;
        private readonly IBookRepository _books;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public RatingService(
            IRepository<Rating> ratings,
            IBookRepository books,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _ratings = ratings;
            _books = books;
            _currentUser = currentUser;
            _uow = uow;
        }

        /// <summary>Creates or updates the caller's rating, then returns the refreshed summary.</summary>
        public async Task<RatingSummaryDto> RateAsync(
            int bookId, RateBookDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Value < MinValue || dto.Value > MaxValue)
            {
                throw new ValidationAppException(RatingErrors.OutOfRange);
            }

            await EnsureBookExistsAsync(bookId, cancellationToken);
            var userId = _currentUser.RequireUserId();

            var existing = await _ratings.FirstOrDefaultAsync(
                r => r.BookId == bookId && r.UserId == userId, cancellationToken);

            if (existing is null)
            {
                await _ratings.AddAsync(
                    new Rating { BookId = bookId, UserId = userId, Value = dto.Value }, cancellationToken);
            }
            else
            {
                existing.Value = dto.Value;
                existing.DateCreated = DateTime.UtcNow;
                _ratings.Update(existing);
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return await GetSummaryAsync(bookId, cancellationToken);
        }

        public async Task<RatingSummaryDto> GetSummaryAsync(int bookId, CancellationToken cancellationToken = default)
        {
            await EnsureBookExistsAsync(bookId, cancellationToken);

            var (average, count) = await _books.GetRatingSummaryAsync(bookId, cancellationToken);

            int? myRating = null;
            if (_currentUser.UserId is int userId)
            {
                var mine = await _ratings.FirstOrDefaultAsync(
                    r => r.BookId == bookId && r.UserId == userId, cancellationToken);
                myRating = mine?.Value;
            }

            return new RatingSummaryDto { Average = average, Count = count, MyRating = myRating };
        }

        public async Task DeleteAsync(int bookId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.RequireUserId();

            var rating = await _ratings.FirstOrDefaultAsync(
                r => r.BookId == bookId && r.UserId == userId, cancellationToken)
                ?? throw new NotFoundException(RatingErrors.NotFound);

            _ratings.Remove(rating);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureBookExistsAsync(int bookId, CancellationToken cancellationToken)
        {
            if (!await _books.ExistsAsync(b => b.Id == bookId && b.IsVisible, cancellationToken))
            {
                throw new NotFoundException(BookErrors.NotFound);
            }
        }
    }
}
