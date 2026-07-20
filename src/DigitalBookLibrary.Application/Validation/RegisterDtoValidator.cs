using DigitalBookLibrary.Application.DTOs.Auth;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Username).NotEmpty().WithErrorCode("USERNAME_REQUIRED")
                .MinimumLength(3).WithErrorCode("USERNAME_TOO_SHORT")
                .MaximumLength(100).WithErrorCode("USERNAME_TOO_LONG");

            RuleFor(x => x.Email).NotEmpty().WithErrorCode("EMAIL_REQUIRED")
                .EmailAddress().WithErrorCode("EMAIL_INVALID")
                .MaximumLength(256).WithErrorCode("EMAIL_TOO_LONG");

            RuleFor(x => x.Password).NotEmpty().WithErrorCode("PASSWORD_REQUIRED")
                .MinimumLength(6).WithErrorCode("PASSWORD_TOO_SHORT")
                .MaximumLength(100).WithErrorCode("PASSWORD_TOO_LONG");
        }
    }
}
