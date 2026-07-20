using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Comments;
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
    /// <summary>Threaded book comments. Authors edit/delete their own; admins moderate any.</summary>
    public class CommentService
    {
        private readonly ICommentRepository _comments;
        private readonly IBookRepository _books;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;
        private readonly IValidator<SaveCommentDto> _validator;

        public CommentService(
            ICommentRepository comments,
            IBookRepository books,
            ICurrentUser currentUser,
            IUnitOfWork uow,
            IValidator<SaveCommentDto> validator)
        {
            _comments = comments;
            _books = books;
            _currentUser = currentUser;
            _uow = uow;
            _validator = validator;
        }

        /// <summary>The book's comments as a thread (replies nested under their parent).</summary>
        public async Task<List<CommentDto>> GetThreadAsync(int bookId, CancellationToken cancellationToken = default)
        {
            await EnsureBookExistsAsync(bookId, cancellationToken);

            var comments = await _comments.GetByBookAsync(bookId, cancellationToken);
            return comments.ToThread();
        }

        public async Task<CommentDto> AddAsync(
            int bookId, SaveCommentDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);
            await EnsureBookExistsAsync(bookId, cancellationToken);

            var userId = _currentUser.RequireUserId();

            if (dto.ParentCommentId is int parentId)
            {
                // A reply must point at a comment on the same book.
                var parent = await _comments.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
                if (parent is null || parent.BookId != bookId)
                {
                    throw new ValidationAppException(CommentErrors.ParentInvalid);
                }
            }

            var comment = new Comment
            {
                BookId = bookId,
                UserId = userId,
                Text = dto.Text.Trim(),
                ParentCommentId = dto.ParentCommentId
            };

            await _comments.AddAsync(comment, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return await ReloadDtoAsync(comment.Id, cancellationToken);
        }

        public async Task<CommentDto> UpdateAsync(
            int commentId, SaveCommentDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var comment = await _comments.GetByIdAsync(commentId, cancellationToken)
                ?? throw new NotFoundException(CommentErrors.NotFound);

            EnsureCanModify(comment);

            comment.Text = dto.Text.Trim();
            _comments.Update(comment);
            await _uow.SaveChangesAsync(cancellationToken);

            return await ReloadDtoAsync(comment.Id, cancellationToken);
        }

        /// <summary>
        /// Re-reads the comment with its author so the response carries the user name — a freshly
        /// added/updated entity has no User navigation loaded.
        /// </summary>
        private async Task<CommentDto> ReloadDtoAsync(int commentId, CancellationToken cancellationToken)
        {
            var saved = await _comments.GetByIdWithUserAsync(commentId, cancellationToken)
                ?? throw new NotFoundException(CommentErrors.NotFound);

            return saved.ToDto();
        }

        public async Task DeleteAsync(int commentId, CancellationToken cancellationToken = default)
        {
            var comment = await _comments.GetByIdAsync(commentId, cancellationToken)
                ?? throw new NotFoundException(CommentErrors.NotFound);

            EnsureCanModify(comment);

            // Replies reference this comment (Restrict), so remove them first.
            var replies = await _comments.FindAsync(c => c.ParentCommentId == commentId, cancellationToken);
            foreach (var reply in replies)
            {
                var tracked = await _comments.GetByIdAsync(reply.Id, cancellationToken);
                if (tracked is not null)
                {
                    _comments.Remove(tracked);
                }
            }

            _comments.Remove(comment);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Only the comment's author or an admin may edit/delete it.</summary>
        private void EnsureCanModify(Comment comment)
        {
            if (_currentUser.IsInRole(Roles.Admin))
            {
                return;
            }

            if (comment.UserId != _currentUser.RequireUserId())
            {
                throw new ForbiddenException(CommentErrors.AccessDenied);
            }
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
