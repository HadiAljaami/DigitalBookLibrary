using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>Authenticated but not permitted (role/ownership). Mapped to HTTP 403.</summary>
    public sealed class ForbiddenException : AppException
    {
        public ForbiddenException(Error error) : base(error) { }
    }
}
