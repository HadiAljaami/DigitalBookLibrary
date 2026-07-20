using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Ratings;
using DigitalBookLibrary.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Route("api/books/{bookId:int}/rating")]
    public class RatingsController : ControllerBase
    {
        private readonly RatingService _ratings;

        public RatingsController(RatingService ratings) => _ratings = ratings;

        /// <summary>Rate a book 1–5. Rating again updates the existing rating.</summary>
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Rate(int bookId, RateBookDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _ratings.RateAsync(bookId, dto, cancellationToken), ResponseCodes.Updated));

        /// <summary>Average rating and count (plus the caller's own rating when signed in).</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(int bookId, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _ratings.GetSummaryAsync(bookId, cancellationToken)));

        /// <summary>Remove the caller's own rating.</summary>
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete(int bookId, CancellationToken cancellationToken)
        {
            await _ratings.DeleteAsync(bookId, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }
    }
}
