using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public enum PostType
{
    JobPosted = 0,
    BidAccepted = 1,
    GeneralPost = 2
}

public class SocialPost
{
    [Key]
    public int PostId { get; set; }

    [Required]
    public int UserId { get; set; } 

    [Required]
    public PostType Type { get; set; }

    public int? JobId { get; set; } 
    public string JobTitle { get; set; } = string.Empty;
    public string? SecondPartyName { get; set; } 
    
    [Column(TypeName = "TEXT")]
    public string? Content { get; set; } 

    // ✅ ADD THESE THREE NEW FIELDS
    public string? ImageUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? DocumentName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ICollection<PostLike>? Likes { get; set; }
    public ICollection<PostComment>? Comments { get; set; }
}