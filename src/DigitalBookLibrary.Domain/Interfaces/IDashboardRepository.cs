using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Domain.Interfaces
{
    /// <summary>
    /// Read-only aggregate queries behind the admin dashboard (docs/05 §8b, §8c).
    /// Unlike the other repositories this one is not tied to a single entity — every method spans
    /// several tables — so it does not extend <see cref="IRepository{T}"/>. Implemented in
    /// Infrastructure with EF Core projections, returning materialized results so the Application
    /// layer never touches EF.
    /// </summary>
    public interface IDashboardRepository
    {
        Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TopBook>> GetTopBooksAsync(
            TopBooksMetric metric, int take, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RecentActivity>> GetRecentAsync(
            RecentActivityType type, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// <paramref name="to"/> is exclusive. Empty buckets are included, so the series is gap-free.
        /// Buckets are floored to the interval, so a <paramref name="from"/> that falls mid-bucket
        /// yields a partial (under-counted) first bucket — callers wanting whole buckets should pass a
        /// boundary date, as the service's own defaults do.
        /// </summary>
        Task<IReadOnlyList<ActivityPoint>> GetActivitySeriesAsync(
            DateTime from, DateTime to, SeriesInterval interval, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DistributionSlice>> GetDistributionAsync(
            DistributionBy by, CancellationToken cancellationToken = default);

        /// <summary>Users with their roles loaded.</summary>
        Task<PagedResult<UserAccount>> GetPagedUsersAsync(
            AdminUserQueryOptions options, CancellationToken cancellationToken = default);

        Task<PagedResult<AuditEntry>> GetPagedAuditAsync(
            AuditQueryOptions options, CancellationToken cancellationToken = default);
    }
}
