using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>Authentication failed or is missing. Mapped to HTTP 401.</summary>
    public sealed class UnauthorizedAppException : AppException
    {
        public UnauthorizedAppException(Error error) : base(error) { }
    }
}
