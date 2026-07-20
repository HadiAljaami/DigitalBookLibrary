namespace DigitalBookLibrary.Application.DTOs.Auth
{
    public sealed class LoginDto
    {
        /// <summary>Email or username.</summary>
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
