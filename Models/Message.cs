using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class Message
{
    [Key]
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;

    // Navigation
    [JsonIgnore]
    public Conversation? Conversation { get; set; }
    
    [JsonIgnore]
    public User? Sender { get; set; }
}