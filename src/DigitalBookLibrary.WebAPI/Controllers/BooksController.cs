using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Books;
using DigitalBookLibrary.Application.Services;
using DigitalBookLibrary.Domain.Common;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController : ControllerBase
    {
        private readonly BookService _books;

        public BooksController(BookService books) => _books = books;

        /// <summary>List books with pagination, filtering, sorting and search.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] BookQueryOptions options, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _books.GetPagedAsync(options, cancellationToken)));

        /// <summary>Get a book's details, including its average rating.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _books.GetByIdAsync(id, cancellationToken)));

        /// <summary>Create a book. Admins add to the library; other users own what they upload.</summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(SaveBookDto dto, CancellationToken cancellationToken)
        {
            var created = await _books.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResponse.Ok(created, ResponseCodes.Created));
        }

        /// <summary>Update a book (Admin, or the publisher who owns it).</summary>
        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, SaveBookDto dto, CancellationToken cancellationToken)
            => Ok(ApiResponse.Ok(await _books.UpdateAsync(id, dto, cancellationToken), ResponseCodes.Updated));

        /// <summary>Delete a book (Admin, or the publisher who owns it).</summary>
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _books.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Deleted));
        }

        /// <summary>Show/hide a book in the public catalog (Admin only).</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/visibility")]
        public async Task<IActionResult> SetVisibility(int id, SetFlagDto dto, CancellationToken cancellationToken)
        {
            await _books.SetVisibilityAsync(id, dto.Value, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Updated));
        }

        /// <summary>Mark a book available/unavailable for download (Admin only).</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:int}/availability")]
        public async Task<IActionResult> SetAvailability(int id, SetFlagDto dto, CancellationToken cancellationToken)
        {
            await _books.SetAvailabilityAsync(id, dto.Value, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Updated));
        }

        /// <summary>Upload/replace the book's PDF (Admin, or the publisher who owns it).</summary>
        [Authorize]
        [HttpPost("{id:int}/file")]
        [RequestSizeLimit(FileValidation.MaxPdfBytes)]
        public async Task<IActionResult> UploadPdf(int id, IFormFile file, CancellationToken cancellationToken)
        {
            await _books.UploadPdfAsync(id, ToUploadRequest(file), cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.Updated));
        }

        /// <summary>Upload/replace the book's cover image (Admin, or the publisher who owns it).</summary>
        [Authorize]
        [HttpPost("{id:int}/cover")]
        [RequestSizeLimit(FileValidation.MaxImageBytes)]
        public async Task<IActionResult> UploadCover(int id, IFormFile file, CancellationToken cancellationToken)
        {
            await _books.UploadCoverAsync(id, ToUploadRequest(file), cancellationToken);
            return Ok(ApiResponse.Ok(new { imageUrl = $"/api/books/{id}/cover" }, ResponseCodes.Updated));
        }

        /// <summary>Stream the book's cover image (public).</summary>
        [HttpGet("{id:int}/cover")]
        public async Task<IActionResult> GetCover(int id, CancellationToken cancellationToken)
        {
            var (content, contentType) = await _books.OpenCoverAsync(id, cancellationToken);
            return File(content, contentType);
        }

        /// <summary>
        /// Converts ASP.NET's IFormFile into the framework-agnostic request the Application layer
        /// understands — IFormFile must never cross the boundary.
        /// </summary>
        private static FileUploadRequest ToUploadRequest(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                throw new ValidationAppException(FileErrors.Required);
            }

            return new FileUploadRequest
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };
        }
    }
}
