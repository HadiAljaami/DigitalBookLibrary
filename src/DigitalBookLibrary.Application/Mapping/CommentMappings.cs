using DigitalBookLibrary.Application.DTOs.Comments;
using DigitalBookLibrary.Domain.Entities;

namespace DigitalBookLibrary.Application.Mapping
{
    /// <summary>Manual entity → DTO mapping for comments (no AutoMapper).</summary>
    public static class CommentMappings
    {
        public static CommentDto ToDto(this Comment comment) => new()
        {
            Id = comment.Id,
            Text = comment.Text,
            UserId = comment.UserId,
            UserName = comment.User?.Username ?? string.Empty,
            ParentCommentId = comment.ParentCommentId,
            DateCreated = comment.DateCreated
        };

        /// <summary>
        /// Assembles the reply thread in memory from one flat query — avoids N+1 recursive DB calls.
        /// </summary>
        public static List<CommentDto> ToThread(this IEnumerable<Comment> comments)
        {
            var nodes = comments.ToDictionary(c => c.Id, c => c.ToDto());
            var roots = new List<CommentDto>();

            foreach (var node in nodes.Values.OrderBy(n => n.DateCreated))
            {
                if (node.ParentCommentId is int parentId && nodes.TryGetValue(parentId, out var parent))
                {
                    parent.Replies.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
    }
}
