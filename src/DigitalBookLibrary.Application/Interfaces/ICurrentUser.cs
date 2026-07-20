namespace DigitalBookLibrary.Application.Interfaces
{
    /// <summary>
    /// Exposes the authenticated user for the current request. Implemented in WebAPI by reading
    /// claims from <c>HttpContext</c>, so this layer stays free of HTTP/ClaimsPrincipal types.
    /// </summary>
    /// <remarks>
    /// <see cref="UserId"/> is intentionally nullable and never throws: the catalog has public
    /// endpoints where an anonymous request is valid. Endpoints that *require* a signed-in user
    /// should call <c>RequireUserId()</c> (see <see cref="CurrentUserExtensions"/>), which centralises
    /// the "must be authenticated" check in one place.
    /// </remarks>
    public interface ICurrentUser
    {
        /// <summary>The authenticated user's id, or <c>null</c> for an anonymous request.</summary>
        int? UserId { get; }

        bool IsAuthenticated { get; }

        bool IsInRole(string role);

        /// <summary>The caller's IP address, recorded on audit entries. Null outside a request.</summary>
        string? IpAddress { get; }
    }
}
