using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/saved-books")]
    public class SavedBooksController : ControllerBase
    {
        private readonly SavedBookService _saved;

        public SavedBooksController(SavedBookService saved) => _saved = saved;

        /// <summary>The caller's saved books (most recently saved first).</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _saved.GetPagedAsync(pagination, cancellationToken)));

        /// <summary>Save a book (idempotent — saving twice is a no-op).</summary>
        [HttpPost("{bookId:int}")]
        public async Task<IActionResult> Save(int bookId, CancellationToken cancellationToken)
        {
            await _saved.SaveAsync(bookId, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Created));
        }

        /// <summary>Remove a book from the saved list.</summary>
        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> Unsave(int bookId, CancellationToken cancellationToken)
        {
            await _saved.UnsaveAsync(bookId, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }
    }
}
