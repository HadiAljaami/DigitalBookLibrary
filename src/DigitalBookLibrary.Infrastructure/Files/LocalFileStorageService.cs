using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalBookLibrary.Infrastructure.Files
{
    /// <summary>
    /// Stores uploads on the local disk under <see cref="FileStorageOptions.RootPath"/>.
    /// Returns an opaque storage key (e.g. "books/{guid}.pdf") — never the client's filename, which
    /// is untrusted and could contain path traversal.
    /// </summary>
    public sealed class LocalFileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IOptions<FileStorageOptions> options, ILogger<LocalFileStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> SaveAsync(
            FileUploadRequest file, string folder, CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var key = $"{folder}/{Guid.NewGuid():N}{extension}";
            var fullPath = ResolvePhysicalPath(key);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            try
            {
                await using var output = File.Create(fullPath);
                await file.Content.CopyToAsync(output, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Failed writing upload to {Path}", fullPath);
                throw new ConflictException(FileErrors.SaveFailed);
            }

            return key;
        }

        public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
        {
            var fullPath = ResolvePhysicalPath(storedPath);
            if (!File.Exists(fullPath))
            {
                throw new NotFoundException(BookErrors.FileMissing);
            }

            Stream stream = File.OpenRead(fullPath);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default)
        {
            var fullPath = ResolvePhysicalPath(storedPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Resolves a storage key to a physical path and refuses anything that escapes the root
        /// (path-traversal guard).
        /// </summary>
        private string ResolvePhysicalPath(string key)
        {
            var root = Path.GetFullPath(_options.RootPath);
            var fullPath = Path.GetFullPath(Path.Combine(root, key));

            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationAppException(FileErrors.TypeNotAllowed);
            }

            return fullPath;
        }
    }
}
