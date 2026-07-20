using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Interfaces
{
    /// <summary>
    /// Issues JWT access tokens and refresh tokens. Implemented in Infrastructure (which owns the
    /// signing key and lifetimes), so the Application layer stays free of options/crypto details.
    /// Refresh tokens are returned raw once and persisted only as a hash.
    /// </summary>
    public interface IJwtProvider
    {
        (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
            UserAccount user, IEnumerable<string> roles);

        /// <summary>Creates a new refresh token: the raw value (returned to the client once), its hash, and expiry.</summary>
        (string Raw, string Hash, DateTime ExpiresAtUtc) GenerateRefreshToken();

        /// <summary>Hashes an incoming raw refresh token so it can be matched against a stored hash.</summary>
        string HashRefreshToken(string rawRefreshToken);
    }
}
