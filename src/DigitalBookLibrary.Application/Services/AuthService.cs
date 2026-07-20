using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Auth;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Mapping;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>Registration, login, refresh-token rotation, logout, and profile retrieval.</summary>
    public class AuthService
    {
        private const string MemberRoleName = "Member";

        private readonly IUserRepository _users;
        private readonly IRepository<Role> _roles;
        private readonly IRepository<RefreshToken> _refreshTokens;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtProvider _jwt;
        private readonly IUnitOfWork _uow;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;

        public AuthService(
            IUserRepository users,
            IRepository<Role> roles,
            IRepository<RefreshToken> refreshTokens,
            IPasswordHasher hasher,
            IJwtProvider jwt,
            IUnitOfWork uow,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator)
        {
            _users = users;
            _roles = roles;
            _refreshTokens = refreshTokens;
            _hasher = hasher;
            _jwt = jwt;
            _uow = uow;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
        }

        public async Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
        {
            await _registerValidator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var username = dto.Username.Trim();
            var email = dto.Email.Trim();

            if (await _users.ExistsAsync(u => u.Email == email, cancellationToken))
            {
                throw new ConflictException(UserErrors.EmailInUse);
            }

            if (await _users.ExistsAsync(u => u.Username == username, cancellationToken))
            {
                throw new ConflictException(UserErrors.UsernameInUse);
            }

            var memberRole = await _roles.FirstOrDefaultAsync(r => r.Name == MemberRoleName, cancellationToken)
                ?? throw new NotFoundException(new Error("ROLE_NOT_FOUND", "The seeded 'Member' role is missing."));

            var user = new UserAccount
            {
                Username = username,
                Email = email,
                PasswordHash = _hasher.Hash(dto.Password)
            };
            user.UserRoles.Add(new UserRole { Role = memberRole });

            await _users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return user.ToDto();
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            await _loginValidator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var user = await _users.GetByEmailOrUsernameAsync(dto.Identifier.Trim(), cancellationToken)
                ?? throw new UnauthorizedAppException(AuthErrors.InvalidCredentials);

            if (!_hasher.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAppException(AuthErrors.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException(UserErrors.Inactive);
            }

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResultDto> RefreshAsync(RefreshRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                throw new UnauthorizedAppException(AuthErrors.RefreshTokenInvalid);
            }

            var hash = _jwt.HashRefreshToken(dto.RefreshToken);
            var token = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
                ?? throw new UnauthorizedAppException(AuthErrors.RefreshTokenInvalid);

            if (token.RevokedAt is not null)
            {
                throw new UnauthorizedAppException(AuthErrors.RefreshTokenRevoked);
            }

            if (DateTime.UtcNow >= token.ExpiresAt)
            {
                throw new UnauthorizedAppException(AuthErrors.RefreshTokenExpired);
            }

            var user = await _users.GetByIdWithRolesAsync(token.UserId, cancellationToken)
                ?? throw new UnauthorizedAppException(AuthErrors.RefreshTokenInvalid);

            if (!user.IsActive)
            {
                throw new ForbiddenException(UserErrors.Inactive);
            }

            // Rotate: revoke the used token and issue a fresh pair.
            var newRefresh = _jwt.GenerateRefreshToken();
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByTokenHash = newRefresh.Hash;
            _refreshTokens.Update(token);

            await _refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefresh.Hash,
                ExpiresAt = newRefresh.ExpiresAtUtc
            }, cancellationToken);

            var access = _jwt.GenerateAccessToken(user, user.RoleNames());
            await _uow.SaveChangesAsync(cancellationToken);

            return new AuthResultDto
            {
                AccessToken = access.Token,
                AccessTokenExpiresAt = access.ExpiresAtUtc,
                RefreshToken = newRefresh.Raw,
                User = user.ToDto()
            };
        }

        public async Task LogoutAsync(RefreshRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return;
            }

            var hash = _jwt.HashRefreshToken(dto.RefreshToken);
            var token = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
            if (token is null || token.RevokedAt is not null)
            {
                return;
            }

            token.RevokedAt = DateTime.UtcNow;
            _refreshTokens.Update(token);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _users.GetByIdWithRolesAsync(userId, cancellationToken)
                ?? throw new NotFoundException(UserErrors.NotFound);

            return user.ToProfileDto();
        }

        private async Task<AuthResultDto> IssueTokensAsync(UserAccount user, CancellationToken cancellationToken)
        {
            var access = _jwt.GenerateAccessToken(user, user.RoleNames());
            var refresh = _jwt.GenerateRefreshToken();

            await _refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refresh.Hash,
                ExpiresAt = refresh.ExpiresAtUtc
            }, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new AuthResultDto
            {
                AccessToken = access.Token,
                AccessTokenExpiresAt = access.ExpiresAtUtc,
                RefreshToken = refresh.Raw,
                User = user.ToDto()
            };
        }
    }
}
