using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>
    /// Base for all application/business exceptions. Transport-agnostic by design:
    /// it carries the semantic <see cref="Error"/> (code + description) but NO HTTP status code.
    /// Mapping an exception type to an HTTP status is done only in the WebAPI middleware.
    /// </summary>
    public abstract class AppException : Exception
    {
        public Error Error { get; }

        protected AppException(Error error) : base(error.Description) => Error = error;
    }
}
