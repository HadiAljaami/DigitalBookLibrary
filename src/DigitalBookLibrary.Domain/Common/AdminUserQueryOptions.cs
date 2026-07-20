namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>Filter/search options for the admin user list (FR-DASH-6).</summary>
    public sealed class AdminUserQueryOptions : PaginationParams
    {
        /// <summary>Free-text search over username and email.</summary>
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>Only users holding this role (e.g. "Admin").</summary>
        public string? Role { get; set; }
    }
}
