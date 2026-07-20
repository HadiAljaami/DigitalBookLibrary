using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>A business rule/state conflict (e.g. book not available). Mapped to HTTP 409.</summary>
    public sealed class ConflictException : AppException
    {
        public ConflictException(Error error) : base(error) { }
    }
}
