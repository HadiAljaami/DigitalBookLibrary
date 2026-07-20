using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>A requested resource does not exist. Mapped to HTTP 404 in the middleware.</summary>
    public sealed class NotFoundException : AppException
    {
        public NotFoundException(Error error) : base(error) { }
    }
}
