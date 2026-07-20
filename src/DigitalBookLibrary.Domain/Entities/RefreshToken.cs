namespace DigitalBookLibrary.Domain.Entities
{
    /// <summary>
    /// A persisted refresh token used to rotate JWT access tokens.
    /// The raw token is never stored — only its hash — and tokens are revocable and rotated on use.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        /// <summary>Hash of the raw refresh token. The raw value is returned to the client only once.</summary>
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        /// <summary>Hash of the token that replaced this one (rotation chain), if any.</summary>
        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }

        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

        public UserAccount? User { get; set; }
    }
}
