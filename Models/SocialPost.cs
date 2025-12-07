using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum PostType
{
    JobPosted = 0,
    BidAccepted = 1
}

public class SocialPost
{
    [Key]
    public int PostId { get; set; }

    [Required]
    public int UserId { get; set; } // The "Actor" (e.g., the Client who posted, or Freelancer who got hired)

    [Required]
    public PostType Type { get; set; }

    public int? JobId { get; set; } // Link to the job for clicking

    // Snapshot data to avoid deep nesting queries
    public string JobTitle { get; set; } = string.Empty;
    public string? SecondPartyName { get; set; } // e.g., The Client name if the actor is Freelancer

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    
    // Interactions
    public ICollection<PostLike>? Likes { get; set; }
    public ICollection<PostComment>? Comments { get; set; }
}