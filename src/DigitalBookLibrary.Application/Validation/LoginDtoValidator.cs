using DigitalBookLibrary.Application.DTOs.Auth;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Identifier).NotEmpty().WithErrorCode("IDENTIFIER_REQUIRED");
            RuleFor(x => x.Password).NotEmpty().WithErrorCode("PASSWORD_REQUIRED");
        }
    }
}
