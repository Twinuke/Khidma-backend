using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class CommentLike
{
    [Key]
    public int LikeId { get; set; }
    public int CommentId { get; set; }
    public int UserId { get; set; }

    [JsonIgnore] public PostComment? Comment { get; set; }
    [JsonIgnore] public User? User { get; set; }
}