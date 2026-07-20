namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>Authentication & token errors. See docs/06-Error-Codes.md §2.</summary>
    public static class AuthErrors
    {
        public static readonly Error InvalidCredentials =
            new("AUTH_INVALID_CREDENTIALS", "Email/username or password is incorrect.");

        public static readonly Error TokenInvalid =
            new("AUTH_TOKEN_INVALID", "Access token is malformed or its signature is invalid.");

        public static readonly Error TokenExpired =
            new("AUTH_TOKEN_EXPIRED", "Access token has expired.");

        public static readonly Error RefreshTokenInvalid =
            new("AUTH_REFRESH_TOKEN_INVALID", "Refresh token was not found or does not match.");

        public static readonly Error RefreshTokenExpired =
            new("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token is past its expiry date.");

        public static readonly Error RefreshTokenRevoked =
            new("AUTH_REFRESH_TOKEN_REVOKED", "Refresh token has already been revoked or rotated.");

        public static readonly Error PasswordTooWeak =
            new("AUTH_PASSWORD_TOO_WEAK", "Password does not meet the complexity policy.");
    }
}
