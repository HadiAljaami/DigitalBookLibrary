using System.Linq.Expressions;
using DigitalBookLibrary.Application.DTOs.Categories;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    public class CategoryServiceTests
    {
        private readonly IRepository<Category> _categories = Substitute.For<IRepository<Category>>();
        private readonly IRepository<Book> _books = Substitute.For<IRepository<Book>>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
        private readonly IValidator<SaveCategoryDto> _validator = Substitute.For<IValidator<SaveCategoryDto>>();

        private CategoryService CreateSut() => new(_categories, _books, _uow, _validator);

        // T-8 — a category that still has books cannot be deleted.
        [Fact]
        public async Task DeleteAsync_CategoryHasBooks_ThrowsAndDoesNotDelete()
        {
            var category = new Category { Id = 1, Name = "Programming" };
            _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);
            var sut = CreateSut();

            var act = () => sut.DeleteAsync(1, CancellationToken.None);

            var ex = await Should.ThrowAsync<ConflictException>(act);
            ex.Error.Code.ShouldBe("CATEGORY_HAS_BOOKS");
            _categories.DidNotReceive().Remove(Arg.Any<Category>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // T-8 — nor one that still has child categories.
        [Fact]
        public async Task DeleteAsync_CategoryHasChildren_ThrowsAndDoesNotDelete()
        {
            var category = new Category { Id = 1, Name = "Programming" };
            _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);   // no books...
            _categories.ExistsAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);    // ...but has children
            var sut = CreateSut();

            var act = () => sut.DeleteAsync(1, CancellationToken.None);

            var ex = await Should.ThrowAsync<ConflictException>(act);
            ex.Error.Code.ShouldBe("CATEGORY_HAS_CHILDREN");
            _categories.DidNotReceive().Remove(Arg.Any<Category>());
            await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        // A leaf category with no books deletes cleanly, committed once.
        [Fact]
        public async Task DeleteAsync_EmptyLeafCategory_RemovesAndSaves()
        {
            var category = new Category { Id = 1, Name = "Programming" };
            _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);
            _books.ExistsAsync(Arg.Any<Expression<Func<Book, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);
            _categories.ExistsAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);
            var sut = CreateSut();

            await sut.DeleteAsync(1, CancellationToken.None);

            _categories.Received(1).Remove(category);
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
