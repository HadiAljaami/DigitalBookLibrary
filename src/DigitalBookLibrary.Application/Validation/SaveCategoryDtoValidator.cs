using DigitalBookLibrary.Application.DTOs.Categories;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class SaveCategoryDtoValidator : AbstractValidator<SaveCategoryDto>
    {
        public SaveCategoryDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithErrorCode("CATEGORY_NAME_REQUIRED")
                .MaximumLength(150).WithErrorCode("CATEGORY_NAME_TOO_LONG");
        }
    }
}
