using DigitalBookLibrary.Application.DTOs.Dashboard;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual read-model → DTO mapping for the admin dashboard (no AutoMapper).</summary>
    public static class DashboardMappings
    {
        /// <summary>A feed row shows a preview, not a wall of text; the full comment lives on the book page.</summary>
        private const int SubjectPreviewLength = 140;

        public static DashboardSummaryDto ToDto(this DashboardSummary summary)
            => new(summary.Books, summary.Users, summary.Authors,
                   summary.Categories, summary.Downloads, summary.Reads);

        public static TopBookDto ToDto(this TopBook book)
            => new(book.BookId, book.Title, book.AuthorName,
                   book.Downloads, book.Reads, book.AverageRating, book.RatingsCount);

        public static RecentActivityDto ToDto(this RecentActivity activity)
            => new(activity.Type, activity.Id, Preview(activity.Title), activity.Subtitle, activity.When);

        public static ActivityPointDto ToDto(this ActivityPoint point)
            => new(point.Period, point.Downloads, point.Reads);

        public static DistributionSliceDto ToDto(this DistributionSlice slice)
            => new(slice.Label, slice.Count);

        public static AuditLogDto ToDto(this AuditEntry entry)
            => new(entry.Id, entry.EntityName, entry.EntityId, entry.Action,
                   entry.UserId, entry.Username, entry.IpAddress, entry.CreatedAt,
                   entry.OldValues, entry.NewValues);

        public static AdminUserDto ToAdminDto(this UserAccount user)
            => user.ToAdminDto(user.RoleNames());

        /// <summary>For callers that know the role names already — e.g. straight after changing them,
        /// when the new join rows have no Role navigation loaded.</summary>
        public static AdminUserDto ToAdminDto(this UserAccount user, IReadOnlyList<string> roles)
            => new(user.Id, user.Username, user.Email, user.Phone,
                   user.IsActive, user.DateCreated, roles);

        private static string Preview(string value)
            => value.Length <= SubjectPreviewLength
                ? value
                : value[..SubjectPreviewLength].TrimEnd() + "…";
    }
}
