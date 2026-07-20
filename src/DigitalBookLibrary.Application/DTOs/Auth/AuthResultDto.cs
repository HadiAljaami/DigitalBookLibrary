namespace DigitalBookLibrary.Application.DTOs.Auth
{
    /// <summary>Returned on login/refresh. The refresh token raw value is delivered here only once.</summary>
    public sealed class AuthResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }
}
