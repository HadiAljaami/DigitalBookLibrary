using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class BookActivityServiceTests
    {
        private readonly IBookRepository _books = Substitute.For<IBookRepository>();
        private readonly IRepository<BookReadLog> _readLogs = Substitute.For<IRepository<BookReadLog>>();
        private readonly IRepository<BookDownloadLog> _downloadLogs = Substitute.For<IRepository<BookDownloadLog>>();
        private readonly IBookActivityRepository _activity = Substitute.For<IBookActivityRepository>();
        private readonly IFileStorageService _files = Substitute.For<IFileStorageService>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

        private BookActivityService CreateSut(int? userId = 1)
            => new(_books, _readLogs, _downloadLogs, _activity, _files, TestDoubles.CurrentUser(userId), _uow);

        private static Book VisibleBook(bool available)
        {
            var book = new Book { Id = 3, Title = "Test", PdfUrl = "books/test.pdf" };
            book.SetAvailability(available);   // IsVisible defaults to true
            return book;
        }

        // T-3 — an unavailable book cannot be downloaded; nothing is counted or logged.
        [Fact]
        public async Task DownloadAsync_BookNotAvailable_ThrowsAndDoesNotCountOrLog()
        {
            var book = VisibleBook(available: false);
            _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
            var sut = CreateSut();

            var act = () => sut.DownloadAsync(book.Id, CancellationToken.None);

            var ex = await Should.ThrowAsync<ConflictException>(act);
            ex.Error.Code.ShouldBe("BOOK_NOT_AVAILABLE");
            book.DownloadsCount.ShouldBe(0);
            await _downloadLogs.DidNotReceive().AddAsync(Arg.Any<BookDownloadLog>(), Arg.Any<CancellationToken>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // T-4 — a successful download bumps the counter AND writes a log, committed in a single save.
        [Fact]
        public async Task DownloadAsync_AvailableBook_IncrementsCounterAndLogsInOneSave()
        {
            var book = VisibleBook(available: true);
            _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
            _files.OpenReadAsync(book.PdfUrl!, Arg.Any<CancellationToken>()).Returns(new MemoryStream());
            var sut = CreateSut(userId: 9);

            await sut.DownloadAsync(book.Id, CancellationToken.None);

            book.DownloadsCount.ShouldBe(1);
            await _downloadLogs.Received(1).AddAsync(
                Arg.Is<BookDownloadLog>(l => l.BookId == book.Id && l.UserId == 9),
                Arg.Any<CancellationToken>());
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
