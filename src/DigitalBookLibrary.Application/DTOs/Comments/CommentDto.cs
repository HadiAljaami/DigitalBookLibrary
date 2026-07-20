namespace DigitalBookLibrary.Application.DTOs.Comments
{
    public sealed class CommentDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public DateTime DateCreated { get; set; }
        public List<CommentDto> Replies { get; set; } = new();
    }
}
