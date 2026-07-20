using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Common;
using NSubstitute;

namespace DigitalBookLibrary.UnitTests
{
    /// <summary>Small builders for the port fakes every service test needs.</summary>
    internal static class TestDoubles
    {
        /// <summary>An <see cref="ICurrentUser"/> that reports the given id and (optionally) the Admin role.</summary>
        public static ICurrentUser CurrentUser(int? userId, bool isAdmin = false)
        {
            var currentUser = Substitute.For<ICurrentUser>();
            currentUser.UserId.Returns(userId);
            currentUser.IsAuthenticated.Returns(userId is not null);
            currentUser.IsInRole(Roles.Admin).Returns(isAdmin);
            return currentUser;
        }
    }
}
