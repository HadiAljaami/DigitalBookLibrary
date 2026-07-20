using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    /// <summary>"My Library" — the member's read, downloaded and saved books.</summary>
    [ApiController]
    [Authorize]
    [Route("api/me")]
    public class MeController : ControllerBase
    {
        private readonly BookActivityService _activity;
        private readonly SavedBookService _saved;

        public MeController(BookActivityService activity, SavedBookService saved)
        {
            _activity = activity;
            _saved = saved;
        }

        /// <summary>Books the caller has read (each book once, most recent first).</summary>
        [HttpGet("read-books")]
        public async Task<IActionResult> ReadBooks(
            [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _activity.GetReadHistoryAsync(pagination, cancellationToken)));

        /// <summary>Books the caller has downloaded (each book once, most recent first).</summary>
        [HttpGet("downloaded-books")]
        public async Task<IActionResult> DownloadedBooks(
            [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _activity.GetDownloadHistoryAsync(pagination, cancellationToken)));

        /// <summary>The caller's saved books (same data as /api/saved-books).</summary>
        [HttpGet("saved-books")]
        public async Task<IActionResult> SavedBooks(
            [FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _saved.GetPagedAsync(pagination, cancellationToken)));
    }
}
