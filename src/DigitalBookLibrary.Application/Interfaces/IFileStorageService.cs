using DigitalBookLibrary.Application.Common;

namespace DigitalBookLibrary.Application.Interfaces
{
    /// <summary>
    /// Abstracts file persistence (PDFs, cover images). Implemented locally now (wwwroot/uploads),
    /// swappable to cloud storage later without touching services.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Saves the file under <paramref name="folder"/> and returns its stored relative path/URL.</summary>
        Task<string> SaveAsync(FileUploadRequest file, string folder, CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default);

        Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default);
    }
}
