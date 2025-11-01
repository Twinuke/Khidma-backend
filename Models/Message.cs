using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class Message
{
    [Key]
    public int MessageId { get; set; }

    [Required]
    public int SenderId { get; set; }

    [Required]
    public int ReceiverId { get; set; }

    public int? JobId { get; set; }

    [Required]
    [Column(TypeName = "TEXT")]
    public string MessageText { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User? Sender { get; set; }

    [JsonIgnore]
    public User? Receiver { get; set; }

    [JsonIgnore]
    public Job? Job { get; set; }
}

