using DigitalBookLibrary.Application.DTOs.Books;
using FluentValidation;

namespace DigitalBookLibrary.Application.Validation
{
    public sealed class SaveBookDtoValidator : AbstractValidator<SaveBookDto>
    {
        public SaveBookDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithErrorCode("BOOK_TITLE_REQUIRED")
                .MaximumLength(300).WithErrorCode("BOOK_TITLE_TOO_LONG");

            RuleFor(x => x.AuthorId).GreaterThan(0).WithErrorCode("BOOK_AUTHOR_REQUIRED");
            RuleFor(x => x.CategoryId).GreaterThan(0).WithErrorCode("BOOK_CATEGORY_REQUIRED");

            RuleFor(x => x.Description).MaximumLength(4000).WithErrorCode("BOOK_DESCRIPTION_TOO_LONG");
            RuleFor(x => x.Language).MaximumLength(50).WithErrorCode("BOOK_LANGUAGE_TOO_LONG");
            RuleFor(x => x.Pages).GreaterThan(0).When(x => x.Pages is not null)
                .WithErrorCode("BOOK_PAGES_INVALID");
        }
    }
}
