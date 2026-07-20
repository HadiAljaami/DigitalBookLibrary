using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Comments;
using DigitalBookLibrary.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Route("api/books/{bookId:int}/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly CommentService _comments;

        public CommentsController(CommentService comments) => _comments = comments;

        /// <summary>The book's comments as a thread.</summary>
        [HttpGet]
        public async Task<IActionResult> GetThread(int bookId, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _comments.GetThreadAsync(bookId, cancellationToken)));

        /// <summary>Add a comment, or a reply when parentCommentId is set.</summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(int bookId, SaveCommentDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _comments.AddAsync(bookId, dto, cancellationToken), ResponseCodes.Created));

        /// <summary>Edit a comment (author or admin).</summary>
        [Authorize]
        [HttpPut("{commentId:int}")]
        public async Task<IActionResult> Update(
            int commentId, SaveCommentDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _comments.UpdateAsync(commentId, dto, cancellationToken), ResponseCodes.Updated));

        /// <summary>Delete a comment and its replies (author or admin).</summary>
        [Authorize]
        [HttpDelete("{commentId:int}")]
        public async Task<IActionResult> Delete(int commentId, CancellationToken cancellationToken)
        {
            await _comments.DeleteAsync(commentId, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }
    }
}
