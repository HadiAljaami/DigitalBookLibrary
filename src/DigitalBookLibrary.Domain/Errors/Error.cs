namespace DigitalBookLibrary.Domain.Errors
{
    /// <summary>
    /// A structured error with a stable client-facing <see cref="Code"/> and a developer-only
    /// <see cref="Description"/>. The <see cref="Code"/> is the ONLY part sent to the frontend
    /// (for i18n); the <see cref="Description"/> is written to logs only and never leaves the server.
    /// </summary>
    public sealed record Error(string Code, string Description)
    {
        public static readonly Error None = new(string.Empty, string.Empty);
    }
}
