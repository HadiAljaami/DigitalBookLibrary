using DigitalBookLibrary.Application.DTOs.Authors;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class SaveAuthorDtoValidator : AbstractValidator<SaveAuthorDto>
    {
        public SaveAuthorDtoValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithErrorCode("AUTHOR_NAME_REQUIRED")
                .MaximumLength(200).WithErrorCode("AUTHOR_NAME_TOO_LONG");

            RuleFor(x => x.Bio).MaximumLength(2000).WithErrorCode("AUTHOR_BIO_TOO_LONG");
            RuleFor(x => x.Nationality).MaximumLength(100).WithErrorCode("AUTHOR_NATIONALITY_TOO_LONG");
        }
    }
}
