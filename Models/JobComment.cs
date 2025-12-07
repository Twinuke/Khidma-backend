using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class JobComment
{
    [Key]
    public int CommentId { get; set; }

    public int JobId { get; set; }
    public int UserId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [JsonIgnore] public Job? Job { get; set; }
    public User? User { get; set; } // Include user to show name/avatar
}