namespace DigitalBookLibrary.Application.DTOs.Books
{
    /// <summary>Payload for the visibility/availability toggle endpoints.</summary>
    public sealed class SetFlagDto
    {
        public bool Value { get; set; }
    }
}
