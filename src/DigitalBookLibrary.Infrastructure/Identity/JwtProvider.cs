using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DigitalBookLibrary.Infrastructure.Identity
{
    /// <summary>Signs HMAC-SHA256 access tokens and generates/rotates opaque refresh tokens (stored hashed).</summary>
    public sealed class JwtProvider : IJwtProvider
    {
        private readonly JwtOptions _options;

        public JwtProvider(IOptions<JwtOptions> options) => _options = options.Value;

        public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(UserAccount user, IEnumerable<string> roles)
        {
            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwt, expiresAt);
        }

        public (string Raw, string Hash, DateTime ExpiresAtUtc) GenerateRefreshToken()
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var hash = HashRefreshToken(raw);
            var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
            return (raw, hash, expiresAt);
        }

        public string HashRefreshToken(string rawRefreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken));
            return Convert.ToHexString(bytes);
        }
    }
}
