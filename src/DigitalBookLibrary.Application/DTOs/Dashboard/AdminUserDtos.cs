namespace DigitalBookLibrary.Application.DTOs.Dashboard
{
    /// <summary>A row of the admin user table (docs/05 §8c GET /). Never carries the password hash.</summary>
    public sealed record AdminUserDto(
        int Id, string Username, string Email, string? Phone,
        bool IsActive, DateTime DateCreated, IReadOnlyList<string> Roles);

    /// <summary>An audit-trail row (docs/05 §8c GET /audit).</summary>
    public sealed record AuditLogDto(
        int Id, string EntityName, string? EntityId, string Action,
        int? UserId, string? Username, string? IpAddress, DateTime CreatedAt,
        string? OldValues, string? NewValues);

    /// <summary>Body of PATCH /{id}/active.</summary>
    public sealed record SetUserActiveDto
    {
        public bool IsActive { get; init; }
    }

    /// <summary>Body of PATCH /{id}/roles — the complete role set the account should end up with.</summary>
    public sealed record SetUserRolesDto
    {
        public List<string> Roles { get; init; } = new();
    }
}
