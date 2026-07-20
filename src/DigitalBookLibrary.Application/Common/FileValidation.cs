using DigitalBookLibrary.Domain.Errors;
using DigitalBookLibrary.Domain.Exceptions;

namespace DigitalBookLibrary.Application.Common
{
    /// <summary>
    /// Business rules for uploaded files. Checks size, extension, content-type AND the real file
    /// header (magic number) — extension and content-type are client-supplied and trivially spoofed,
    /// so the header is what actually proves the file type.
    /// </summary>
    public static class FileValidation
    {
        public const long MaxPdfBytes = 50L * 1024 * 1024;   // 50 MB
        public const long MaxImageBytes = 5L * 1024 * 1024;  // 5 MB

        private static readonly string[] PdfExtensions = { ".pdf" };
        private static readonly string[] PdfContentTypes = { "application/pdf" };

        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] ImageContentTypes = { "image/jpeg", "image/png", "image/webp" };

        public static async Task ValidatePdfAsync(FileUploadRequest file, CancellationToken cancellationToken = default)
        {
            EnsureBasics(file, PdfExtensions, PdfContentTypes, MaxPdfBytes);

            var header = await ReadHeaderAsync(file.Content, 4, cancellationToken);
            if (!header.AsSpan().SequenceEqual("%PDF"u8))
            {
                throw new ValidationAppException(FileErrors.Corrupted);
            }
        }

        public static async Task ValidateImageAsync(FileUploadRequest file, CancellationToken cancellationToken = default)
        {
            EnsureBasics(file, ImageExtensions, ImageContentTypes, MaxImageBytes);

            var header = await ReadHeaderAsync(file.Content, 12, cancellationToken);
            if (!IsJpeg(header) && !IsPng(header) && !IsWebp(header))
            {
                throw new ValidationAppException(FileErrors.Corrupted);
            }
        }

        /// <summary>Maps a stored file's extension to the content-type used when serving it back.</summary>
        public static string ResolveContentType(string fileNameOrKey) =>
            Path.GetExtension(fileNameOrKey).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

        private static void EnsureBasics(
            FileUploadRequest file, string[] allowedExtensions, string[] allowedContentTypes, long maxBytes)
        {
            if (file.Length <= 0)
            {
                throw new ValidationAppException(FileErrors.Required);
            }

            if (file.Length > maxBytes)
            {
                throw new ValidationAppException(FileErrors.TooLarge);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ValidationAppException(FileErrors.TypeNotAllowed);
            }

            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                throw new ValidationAppException(FileErrors.TypeNotAllowed);
            }
        }

        /// <summary>Reads the first bytes then rewinds, so the caller can still stream the whole file.</summary>
        private static async Task<byte[]> ReadHeaderAsync(Stream stream, int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[count];
            var read = await stream.ReadAtLeastAsync(buffer, count, throwOnEndOfStream: false, cancellationToken);

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            return read < count ? Array.Empty<byte>() : buffer;
        }

        private static bool IsJpeg(byte[] h) => h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;

        private static bool IsPng(byte[] h) => h.Length >= 8 && h.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        private static bool IsWebp(byte[] h) => h.Length >= 12
            && h.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && h.AsSpan(8, 4).SequenceEqual("WEBP"u8);
    }
}
