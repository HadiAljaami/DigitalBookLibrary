namespace DigitalBookLibrary.Application.Common
{
    /// <summary>
    /// A framework-agnostic upload payload. The WebAPI controller converts <c>IFormFile</c> into this
    /// at the edge, so ASP.NET types never leak into the Application layer.
    /// </summary>
    public sealed class FileUploadRequest
    {
        public required Stream Content { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public long Length { get; init; }
    }
}
