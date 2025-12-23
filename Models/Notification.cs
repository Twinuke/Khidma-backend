using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum NotificationType
{
    General = 0,
    BidPlaced = 1,
    BidAccepted = 2,
    Payment = 3,
    System = 4,
    ConnectionRequest = 5,
    PostCreated = 6 // ✅ New post from connection
}

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Stores the ID of the related entity (e.g., JobId, PostId, or ConnectionId).
    /// This was missing and causing the build errors in WorkspaceController.
    /// </summary>
    public int? EntityId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;

    public bool IsRead { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public User? User { get; set; }
}