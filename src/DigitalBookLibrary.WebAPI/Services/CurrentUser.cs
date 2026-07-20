using System.Security.Claims;
using DigitalBookLibrary.Application.Interfaces;

namespace DigitalBookLibrary.WebAPI.Services
{
    /// <summary>
    /// Reads the authenticated user from the current request's claims.
    /// Registered Scoped, so the identifier claim is parsed once per request and reused.
    /// </summary>
    public sealed class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        private bool _resolved;
        private int? _userId;

        public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public int? UserId
        {
            get
            {
                if (!_resolved)
                {
                    _userId = ResolveUserId();
                    _resolved = true;
                }

                return _userId;
            }
        }

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

        public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        /// <summary>
        /// The JWT "sub" claim is remapped to <see cref="ClaimTypes.NameIdentifier"/> by the default
        /// inbound claim mapping; both are read so this keeps working if that mapping is disabled.
        /// </summary>
        private int? ResolveUserId()
        {
            var principal = Principal;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirstValue("sub");

            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
