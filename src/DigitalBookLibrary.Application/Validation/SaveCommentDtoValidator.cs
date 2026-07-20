using DigitalBookLibrary.Application.DTOs.Comments;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class SaveCommentDtoValidator : AbstractValidator<SaveCommentDto>
    {
        public SaveCommentDtoValidator()
        {
            RuleFor(x => x.Text).NotEmpty().WithErrorCode("COMMENT_TEXT_REQUIRED")
                .MaximumLength(2000).WithErrorCode("COMMENT_TEXT_TOO_LONG");
        }
    }
}
