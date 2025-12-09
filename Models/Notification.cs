using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum NotificationType
{
    General = 0,
    BidPlaced = 1,      // Link to Job/Bid
    BidAccepted = 2,    // Link to Contract/Job
    Payment = 3,
    System = 4,
    ConnectionRequest = 5, // Link to User Profile
    SocialLike = 6,        // Link to Post
    SocialComment = 7,     // Link to Post
    SocialReaction = 8     // Link to Post
}

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;

    // ✅ NEW: Stores the ID of the Job, Post, or User related to this event
    public int? RelatedEntityId { get; set; } 

    public bool IsRead { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public User? User { get; set; }
}