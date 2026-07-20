using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    /// <summary>Admin dashboard data (docs/05 §8b). Every endpoint is Admin-only.</summary>
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = Roles.Admin)]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboard;

        public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

        /// <summary>KPI card totals: books, users, authors, categories, downloads, reads.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetSummaryAsync(cancellationToken)));

        /// <summary>Top-N books by downloads (default), reads or rating.</summary>
        [HttpGet("top-books")]
        public async Task<IActionResult> GetTopBooks(
            [FromQuery] string? metric, [FromQuery] int take, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetTopBooksAsync(metric, take, cancellationToken)));

        /// <summary>Recent activity feed: newest users (default), books or comments.</summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent(
            [FromQuery] string? type, [FromQuery] int take, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetRecentAsync(type, take, cancellationToken)));

        /// <summary>
        /// Downloads/reads over time, gap-free for charting. Defaults to the last 30 days
        /// (or 12 months when <c>interval=month</c>); <c>to</c> is exclusive.
        /// </summary>
        [HttpGet("activity-series")]
        public async Task<IActionResult> GetActivitySeries(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? interval,
            CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetActivitySeriesAsync(from, to, interval, cancellationToken)));

        /// <summary>Book counts grouped by category (default) or language.</summary>
        [HttpGet("distribution")]
        public async Task<IActionResult> GetDistribution(
            [FromQuery] string? by, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _dashboard.GetDistributionAsync(by, cancellationToken)));
    }
}
