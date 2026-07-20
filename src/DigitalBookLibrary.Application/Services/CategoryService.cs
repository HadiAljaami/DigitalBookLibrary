using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Categories;
using DigitalBookLibrary.Application.Mapping;
using DigitalBookLibrary.Domain.Entities;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using DigitalBookLibrary.Domain.Interfaces;
using FluentValidation;

namespace DigitalBookLibrary.Application.Services
{
    /// <summary>Category tree management with delete guards.</summary>
    public class CategoryService
    {
        private readonly IRepository<Category> _categories;
        private readonly IRepository<Book> _books;
        private readonly IUnitOfWork _uow;
        private readonly IValidator<SaveCategoryDto> _validator;

        public CategoryService(
            IRepository<Category> categories,
            IRepository<Book> books,
            IUnitOfWork uow,
            IValidator<SaveCategoryDto> validator)
        {
            _categories = categories;
            _books = books;
            _uow = uow;
            _validator = validator;
        }

        /// <summary>All categories as a tree (roots with nested children).</summary>
        public async Task<List<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default)
        {
            var all = await _categories.ListAsync(cancellationToken);
            return all.ToTree();
        }

        public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _categories.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(CategoryErrors.NotFound);

            return category.ToDto();
        }

        public async Task<CategoryDto> CreateAsync(SaveCategoryDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);
            await EnsureParentExistsAsync(dto.ParentCategoryId, cancellationToken);

            var category = new Category
            {
                Name = dto.Name.Trim(),
                ParentCategoryId = dto.ParentCategoryId
            };

            await _categories.AddAsync(category, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return category.ToDto();
        }

        public async Task<CategoryDto> UpdateAsync(
            int id, SaveCategoryDto dto, CancellationToken cancellationToken = default)
        {
            await _validator.ValidateAndThrowAppAsync(dto, cancellationToken);

            var category = await _categories.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(CategoryErrors.NotFound);

            // A category cannot be its own parent, nor sit under one of its own descendants.
            if (dto.ParentCategoryId == id || await WouldCreateCycleAsync(id, dto.ParentCategoryId, cancellationToken))
            {
                throw new ValidationAppException(CategoryErrors.ParentInvalid);
            }

            await EnsureParentExistsAsync(dto.ParentCategoryId, cancellationToken);

            category.Name = dto.Name.Trim();
            category.ParentCategoryId = dto.ParentCategoryId;
            _categories.Update(category);
            await _uow.SaveChangesAsync(cancellationToken);

            return category.ToDto();
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _categories.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(CategoryErrors.NotFound);

            if (await _books.ExistsAsync(b => b.CategoryId == id, cancellationToken))
            {
                throw new ConflictException(CategoryErrors.HasBooks);
            }

            if (await _categories.ExistsAsync(c => c.ParentCategoryId == id, cancellationToken))
            {
                throw new ConflictException(CategoryErrors.HasChildren);
            }

            _categories.Remove(category);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureParentExistsAsync(int? parentId, CancellationToken cancellationToken)
        {
            if (parentId is null)
            {
                return;
            }

            if (!await _categories.ExistsAsync(c => c.Id == parentId, cancellationToken))
            {
                throw new ValidationAppException(CategoryErrors.ParentInvalid);
            }
        }

        /// <summary>Walks up from the candidate parent; if we meet <paramref name="id"/>, it's a cycle.</summary>
        private async Task<bool> WouldCreateCycleAsync(int id, int? parentId, CancellationToken cancellationToken)
        {
            var currentId = parentId;
            while (currentId is not null)
            {
                if (currentId == id)
                {
                    return true;
                }

                var parent = await _categories.GetByIdAsync(currentId.Value, cancellationToken);
                currentId = parent?.ParentCategoryId;
            }

            return false;
        }
    }
}
