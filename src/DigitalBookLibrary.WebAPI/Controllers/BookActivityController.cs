using DigitalBookLibrary.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    /// <summary>
    /// Reading/downloading book files. These are the ONLY way to reach a PDF — files live outside
    /// wwwroot, so access always passes authentication, the availability rule and the counters.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/books/{id:int}")]
    public class BookActivityController : ControllerBase
    {
        private readonly BookActivityService _activity;

        public BookActivityController(BookActivityService activity) => _activity = activity;

        /// <summary>Read a book online (inline). Logs the read and bumps the counter.</summary>
        [HttpGet("read")]
        public async Task<IActionResult> Read(int id, CancellationToken cancellationToken)
        {
            var (content, contentType, _) = await _activity.ReadAsync(id, cancellationToken);
            return File(content, contentType);   // inline — no download filename
        }

        /// <summary>Download a book. Blocked when the book is marked unavailable.</summary>
        [HttpGet("download")]
        public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
        {
            var (content, contentType, fileName) = await _activity.DownloadAsync(id, cancellationToken);
            return File(content, contentType, fileName);
        }
    }
}
