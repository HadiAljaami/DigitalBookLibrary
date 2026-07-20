using System.Security.Cryptography;
using DigitalBookLibrary.Application.Interfaces;

namespace DigitalBookLibrary.Infrastructure.Identity
{
    /// <summary>
    /// PBKDF2 (Rfc2898) password hashing with a per-password random salt. Stored format:
    /// <c>iterations:base64(salt):base64(hash)</c>. Verification is constant-time.
    /// </summary>
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;        // 128-bit
        private const int KeySize = 32;         // 256-bit
        private const int Iterations = 100_000;
        private const char Delimiter = ':';
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
            return string.Join(Delimiter, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public bool Verify(string password, string passwordHash)
        {
            var parts = passwordHash.Split(Delimiter);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);
            var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, hash.Length);
            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
    }
}
