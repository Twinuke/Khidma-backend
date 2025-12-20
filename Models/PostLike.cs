using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class PostLike
{
    [Key]
    public int LikeId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }

    // Stores the type: "Like", "Celebrate", "Support", "Love", "Insightful", or "Funny"
    // If a user removes their reaction, this record is deleted.
    public string? ReactionType { get; set; } 

    [JsonIgnore] public SocialPost? Post { get; set; }
    [JsonIgnore] public User? User { get; set; }
}