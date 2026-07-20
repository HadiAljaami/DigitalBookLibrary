using System.Linq.Expressions;
using DigitalBookLibrary.Application.DTOs.Auth;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly IRepository<Role> _roles = Substitute.For<IRepository<Role>>();
        private readonly IRepository<RefreshToken> _refreshTokens = Substitute.For<IRepository<RefreshToken>>();
        private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
        private readonly IJwtProvider _jwt = Substitute.For<IJwtProvider>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
        private readonly IValidator<RegisterDto> _registerValidator = Substitute.For<IValidator<RegisterDto>>();
        private readonly IValidator<LoginDto> _loginValidator = Substitute.For<IValidator<LoginDto>>();

        public AuthServiceTests()
        {
            // Validation itself is covered by validator tests; here the DTOs are always well-formed.
            _loginValidator.ValidateAsync(Arg.Any<LoginDto>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
            _registerValidator.ValidateAsync(Arg.Any<RegisterDto>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());
        }

        private AuthService CreateSut() => new(
            _users, _roles, _refreshTokens, _hasher, _jwt, _uow, _registerValidator, _loginValidator);

        // T-7 — an unknown identifier is rejected as invalid credentials (not "user not found",
        // which would leak which accounts exist).
        [Fact]
        public async Task LoginAsync_UnknownIdentifier_ThrowsInvalidCredentials()
        {
            _users.GetByEmailOrUsernameAsync("ghost", Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
            var sut = CreateSut();

            var act = () => sut.LoginAsync(new LoginDto { Identifier = "ghost", Password = "x" }, CancellationToken.None);

            var ex = await Should.ThrowAsync<UnauthorizedAppException>(act);
            ex.Error.Code.ShouldBe("AUTH_INVALID_CREDENTIALS");
        }

        // T-7 — a wrong password is likewise AUTH_INVALID_CREDENTIALS.
        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentials()
        {
            var user = new UserAccount { Id = 1, Username = "hadi", PasswordHash = "stored" };
            _users.GetByEmailOrUsernameAsync("hadi", Arg.Any<CancellationToken>()).Returns(user);
            _hasher.Verify("bad", "stored").Returns(false);
            var sut = CreateSut();

            var act = () => sut.LoginAsync(new LoginDto { Identifier = "hadi", Password = "bad" }, CancellationToken.None);

            var ex = await Should.ThrowAsync<UnauthorizedAppException>(act);
            ex.Error.Code.ShouldBe("AUTH_INVALID_CREDENTIALS");
        }

        // T-7 — correct credentials but a deactivated account is forbidden, with its own code.
        [Fact]
        public async Task LoginAsync_InactiveUser_ThrowsForbiddenInactive()
        {
            var user = new UserAccount { Id = 1, Username = "hadi", PasswordHash = "stored", IsActive = false };
            _users.GetByEmailOrUsernameAsync("hadi", Arg.Any<CancellationToken>()).Returns(user);
            _hasher.Verify("right", "stored").Returns(true);
            var sut = CreateSut();

            var act = () => sut.LoginAsync(new LoginDto { Identifier = "hadi", Password = "right" }, CancellationToken.None);

            var ex = await Should.ThrowAsync<ForbiddenException>(act);
            ex.Error.Code.ShouldBe("USER_INACTIVE");
        }

        // T-6 — refreshing rotates the token: the presented one is revoked and linked to a freshly
        // issued replacement, all committed once.
        [Fact]
        public async Task RefreshAsync_ValidToken_RevokesOldIssuesNew()
        {
            var stored = new RefreshToken
            {
                Id = 5,
                UserId = 1,
                TokenHash = "hash-old",
                ExpiresAt = DateTime.UtcNow.AddDays(3)
            };
            var user = new UserAccount { Id = 1, Username = "hadi", IsActive = true };

            _jwt.HashRefreshToken("raw-old").Returns("hash-old");
            _refreshTokens.FirstOrDefaultAsync(
                    Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(stored);
            _users.GetByIdWithRolesAsync(1, Arg.Any<CancellationToken>()).Returns(user);
            _jwt.GenerateRefreshToken().Returns(("raw-new", "hash-new", DateTime.UtcNow.AddDays(7)));
            _jwt.GenerateAccessToken(Arg.Any<UserAccount>(), Arg.Any<IEnumerable<string>>())
                .Returns(("access-jwt", DateTime.UtcNow.AddMinutes(15)));
            var sut = CreateSut();

            var result = await sut.RefreshAsync(new RefreshRequestDto { RefreshToken = "raw-old" }, CancellationToken.None);

            stored.RevokedAt.ShouldNotBeNull();                 // old token revoked
            stored.ReplacedByTokenHash.ShouldBe("hash-new");    // and linked to its replacement
            result.RefreshToken.ShouldBe("raw-new");            // caller gets the new raw token
            await _refreshTokens.Received(1).AddAsync(
                Arg.Is<RefreshToken>(t => t.TokenHash == "hash-new" && t.UserId == 1),
                Arg.Any<CancellationToken>());
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // T-6 — an already-revoked token cannot be exchanged, and nothing is issued.
        [Fact]
        public async Task RefreshAsync_RevokedToken_ThrowsAndIssuesNothing()
        {
            var stored = new RefreshToken
            {
                Id = 5,
                UserId = 1,
                TokenHash = "hash-old",
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                RevokedAt = DateTime.UtcNow.AddMinutes(-1)      // already rotated
            };

            _jwt.HashRefreshToken("raw-old").Returns("hash-old");
            _refreshTokens.FirstOrDefaultAsync(
                    Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(stored);
            var sut = CreateSut();

            var act = () => sut.RefreshAsync(new RefreshRequestDto { RefreshToken = "raw-old" }, CancellationToken.None);

            var ex = await Should.ThrowAsync<UnauthorizedAppException>(act);
            ex.Error.Code.ShouldBe("AUTH_REFRESH_TOKEN_REVOKED");
            await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
