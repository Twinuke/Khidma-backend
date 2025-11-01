using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class ChatbotLog
{
    [Key]
    public int ChatId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [Column(TypeName = "TEXT")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Response { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User? User { get; set; }
}

