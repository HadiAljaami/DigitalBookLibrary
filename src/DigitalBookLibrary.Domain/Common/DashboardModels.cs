namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>KPI card totals (FR-DASH-1).</summary>
    public sealed record DashboardSummary(
        int Books, int Users, int Authors, int Categories, int Downloads, int Reads);

    /// <summary>A book ranked by one of the dashboard metrics (FR-DASH-2).</summary>
    public sealed record TopBook(
        int BookId, string Title, string? AuthorName,
        int Downloads, int Reads, double AverageRating, int RatingsCount);

    /// <summary>
    /// One entry of the recent-activity feed (FR-DASH-3). The shape is deliberately uniform across
    /// users/books/comments so the panel can render a single table.
    /// </summary>
    public sealed record RecentActivity(
        string Type, int Id, string Title, string? Subtitle, DateTime When);

    /// <summary>One bucket of the activity time series (FR-DASH-4).</summary>
    public sealed record ActivityPoint(DateTime Period, int Downloads, int Reads);

    /// <summary>One slice of a books distribution (FR-DASH-5).</summary>
    public sealed record DistributionSlice(string Label, int Count);

    /// <summary>
    /// An audit row with its actor's username resolved (FR-DASH-7). <see cref="Entities.AuditLog"/>
    /// has no User navigation — audit rows outlive the accounts they mention — so the name is joined
    /// in the query rather than lazily walked per row.
    /// </summary>
    public sealed record AuditEntry(
        int Id, string EntityName, string? EntityId, string Action,
        int? UserId, string? Username, string? IpAddress, DateTime CreatedAt,
        string? OldValues, string? NewValues);

    public enum TopBooksMetric { Downloads, Reads, Rating }

    public enum RecentActivityType { Users, Books, Comments }

    public enum SeriesInterval { Day, Month }

    public enum DistributionBy { Category, Language }
}
