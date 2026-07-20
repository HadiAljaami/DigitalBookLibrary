using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Categories;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categories;

        public CategoriesController(CategoryService categories) => _categories = categories;

        /// <summary>Get all categories as a tree.</summary>
        [HttpGet]
        public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _categories.GetTreeAsync(cancellationToken)));

        /// <summary>Get a single category.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _categories.GetByIdAsync(id, cancellationToken)));

        /// <summary>Create a category (Admin only).</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create(SaveCategoryDto dto, CancellationToken cancellationToken)
        {
            var created = await _categories.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResponse.Ok(created, ResponseCodes.Created));
        }

        /// <summary>Update a category (Admin only).</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, SaveCategoryDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _categories.UpdateAsync(id, dto, cancellationToken), ResponseCodes.Updated));

        /// <summary>Delete a category (Admin only). Blocked when it has books or children.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _categories.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }
    }
}
