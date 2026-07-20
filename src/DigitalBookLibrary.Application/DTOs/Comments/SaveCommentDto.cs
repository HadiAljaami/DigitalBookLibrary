namespace DigitalBookLibrary.Application.DTOs.Comments
{
    public sealed class SaveCommentDto
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>Set to reply to an existing comment on the same book.</summary>
        public int? ParentCommentId { get; set; }
    }
}
