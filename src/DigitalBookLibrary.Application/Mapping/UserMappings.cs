using DigitalBookLibrary.Application.DTOs.Auth;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual entity → DTO mapping for user/account (no AutoMapper).</summary>
    public static class UserMappings
    {
        public static List<string> RoleNames(this UserAccount user)
            => user.UserRoles
                   .Select(ur => ur.Role?.Name ?? string.Empty)
                   .Where(name => name.Length > 0)
                   .ToList();

        public static UserDto ToDto(this UserAccount user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = user.RoleNames()
        };

        public static UserProfileDto ToProfileDto(this UserAccount user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.Person?.FullName,
            IsActive = user.IsActive,
            Roles = user.RoleNames()
        };
    }
}
