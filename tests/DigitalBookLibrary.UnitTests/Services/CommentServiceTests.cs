using System.Linq.Expressions;
using DigitalBookLibrary.Application.DTOs.Comments;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class CommentServiceTests
    {
        private readonly ICommentRepository _comments = Substitute.For<ICommentRepository>();
        private readonly IBookRepository _books = Substitute.For<IBookRepository>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
        private readonly IValidator<SaveCommentDto> _validator = Substitute.For<IValidator<SaveCommentDto>>();

        public CommentServiceTests()
            => _validator.ValidateAsync(Arg.Any<SaveCommentDto>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());

        private CommentService CreateSut(int userId, bool isAdmin = false)
            => new(_comments, _books, TestDoubles.CurrentUser(userId, isAdmin), _uow, _validator);

        // T-10 — a member editing someone else's comment is forbidden and nothing is written.
        [Fact]
        public async Task UpdateAsync_NonOwnerNonAdmin_ThrowsAccessDenied()
        {
            var othersComment = new Comment { Id = 10, BookId = 1, UserId = 2, Text = "original" };
            _comments.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(othersComment);
            var sut = CreateSut(userId: 1);   // not the owner (2), not admin

            var act = () => sut.UpdateAsync(10, new SaveCommentDto { Text = "hacked" }, CancellationToken.None);

            var ex = await Should.ThrowAsync<ForbiddenException>(act);
            ex.Error.Code.ShouldBe("COMMENT_ACCESS_DENIED");
            othersComment.Text.ShouldBe("original");   // untouched
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // T-10 — the same guard applies to deletion.
        [Fact]
        public async Task DeleteAsync_NonOwnerNonAdmin_ThrowsAccessDenied()
        {
            var othersComment = new Comment { Id = 10, BookId = 1, UserId = 2, Text = "original" };
            _comments.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(othersComment);
            var sut = CreateSut(userId: 1);

            var act = () => sut.DeleteAsync(10, CancellationToken.None);

            var ex = await Should.ThrowAsync<ForbiddenException>(act);
            ex.Error.Code.ShouldBe("COMMENT_ACCESS_DENIED");
            _comments.DidNotReceive().Remove(Arg.Any<Comment>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // An admin moderates any comment — the ownership guard must let them through.
        [Fact]
        public async Task DeleteAsync_Admin_DeletesAnyComment()
        {
            var othersComment = new Comment { Id = 10, BookId = 1, UserId = 2, Text = "spam" };
            _comments.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(othersComment);
            _comments.FindAsync(Arg.Any<Expression<Func<Comment, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Array.Empty<Comment>());   // no replies to cascade
            var sut = CreateSut(userId: 99, isAdmin: true);

            await sut.DeleteAsync(10, CancellationToken.None);

            _comments.Received(1).Remove(othersComment);
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
