using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class PostComment
{
    [Key]
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public SocialPost? Post { get; set; }
    public User? User { get; set; }
}