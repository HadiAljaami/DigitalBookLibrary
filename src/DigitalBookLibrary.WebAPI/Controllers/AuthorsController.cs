using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Authors;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly AuthorService _authors;

        public AuthorsController(AuthorService authors) => _authors = authors;

        /// <summary>List authors (paged, searchable, sortable).</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] AuthorQueryOptions options, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _authors.GetPagedAsync(options, cancellationToken)));

        /// <summary>Get an author with their books.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _authors.GetByIdAsync(id, cancellationToken)));

        /// <summary>Create an author (Admin only). No user account is created.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create(SaveAuthorDto dto, CancellationToken cancellationToken)
        {
            var created = await _authors.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResponse.Ok(created, ResponseCodes.Created));
        }

        /// <summary>Update an author (Admin only).</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, SaveAuthorDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _authors.UpdateAsync(id, dto, cancellationToken), ResponseCodes.Updated));

        /// <summary>Delete an author (Admin only). Blocked when they still have books.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _authors.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }
    }
}
