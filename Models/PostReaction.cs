using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class PostReaction
{
    [Key]
    public int ReactionId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Reaction { get; set; } = string.Empty;

    [JsonIgnore] public SocialPost? Post { get; set; }
    [JsonIgnore] public User? User { get; set; }
}