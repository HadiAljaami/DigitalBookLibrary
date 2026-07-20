using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using FluentValidation;

namespace DigitalBookLibrary.Application.Common
{
    /// <summary>
    /// Bridges FluentValidation to our error model: turns validation failures into a
    /// <see cref="ValidationAppException"/> carrying stable codes (the rule's ErrorCode) while the
    /// human-readable message goes only to the exception/log.
    /// </summary>
    public static class ValidationExtensions
    {
        public static async Task ValidateAndThrowAppAsync<T>(
            this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            if (result.IsValid)
            {
                return;
            }

            var errors = result.Errors.Select(failure => new Error(
                string.IsNullOrWhiteSpace(failure.ErrorCode) ? "VALIDATION_FAILED" : failure.ErrorCode,
                $"{failure.PropertyName}: {failure.ErrorMessage}"));

            throw new ValidationAppException(errors);
        }
    }
}
