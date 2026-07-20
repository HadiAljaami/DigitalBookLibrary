namespace DigitalBookLibrary.Domain.Entities
{
public class Comment
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public Book? Book { get; set; }
    public UserAccount? User { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}}
