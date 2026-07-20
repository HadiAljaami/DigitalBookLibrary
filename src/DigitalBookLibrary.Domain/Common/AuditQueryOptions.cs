namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>Filter options for the audit trail listing (FR-DASH-7).</summary>
    public sealed class AuditQueryOptions : PaginationParams
    {
        /// <summary>Entity type, e.g. "Book" (exact match).</summary>
        public string? EntityName { get; set; }

        /// <summary>Create | Update | Delete (exact match).</summary>
        public string? Action { get; set; }
    }
}
