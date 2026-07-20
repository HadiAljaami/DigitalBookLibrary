namespace DigitalBookLibrary.Infrastructure.Files
{
    public sealed class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        /// <summary>
        /// Physical root for uploads. Deliberately OUTSIDE wwwroot: book PDFs must not be reachable by
        /// direct URL, or downloads would bypass authentication, the availability rule and the counters.
        /// Everything is served through the API instead.
        /// </summary>
        public string RootPath { get; set; } = "App_Data/uploads";
    }
}
