using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;

namespace DigitalBookLibrary.Application.Interfaces
{
    public static class CurrentUserExtensions
    {
        /// <summary>
        /// Returns the current user's id, throwing <see cref="UnauthorizedAppException"/> when the
        /// request is anonymous or the token carries no usable identifier. Use this on endpoints that
        /// require a signed-in user, instead of repeating a null-check at every call site.
        /// </summary>
        /// <remarks>
        /// This is a method (not a property) because it can fail — the name makes that explicit at the
        /// call site. It lives here as an extension so the throw is implemented exactly once and every
        /// <see cref="ICurrentUser"/> implementation (including test fakes) behaves identically.
        /// </remarks>
        public static int RequireUserId(this ICurrentUser currentUser)
            => currentUser.UserId ?? throw new UnauthorizedAppException(AuthErrors.TokenInvalid);
    }
}
