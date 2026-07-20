namespace DigitalBookLibrary.Application.DTOs.Dashboard
{
    /// <summary>KPI card totals (docs/05 §8b GET /summary).</summary>
    public sealed record DashboardSummaryDto(
        int Books, int Users, int Authors, int Categories, int Downloads, int Reads);

    /// <summary>A row of the top-books table (docs/05 §8b GET /top-books).</summary>
    public sealed record TopBookDto(
        int BookId, string Title, string? AuthorName,
        int Downloads, int Reads, double AverageRating, int RatingsCount);

    /// <summary>An entry of the recent-activity feed (docs/05 §8b GET /recent).</summary>
    public sealed record RecentActivityDto(
        string Type, int Id, string Title, string? Subtitle, DateTime When);

    /// <summary>One point of the activity chart (docs/05 §8b GET /activity-series).</summary>
    public sealed record ActivityPointDto(DateTime Period, int Downloads, int Reads);

    /// <summary>One slice of a distribution chart (docs/05 §8b GET /distribution).</summary>
    public sealed record DistributionSliceDto(string Label, int Count);
}
