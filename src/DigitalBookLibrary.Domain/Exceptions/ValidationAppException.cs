using DigitalBookLibrary.Domain.Errors;

namespace DigitalBookLibrary.Domain.Exceptions
{
    /// <summary>
    /// One or more validation rules failed. Carries every failed <see cref="Error"/> so the client
    /// receives all failing codes at once. Mapped to HTTP 400 in the middleware.
    /// </summary>
    public sealed class ValidationAppException : AppException
    {
        public IReadOnlyList<Error> Errors { get; }

        public ValidationAppException(IEnumerable<Error> errors)
            : base(CommonErrors.Validation)
            => Errors = errors.ToList();

        public ValidationAppException(Error error)
            : base(CommonErrors.Validation)
            => Errors = new[] { error };
    }
}
