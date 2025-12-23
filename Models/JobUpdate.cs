using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

// ✅ Define Status Enum
public enum UpdateStatus { Pending = 0, Approved = 1, Dismissed = 2 }

public class JobUpdate
{
    [Key]
    public int UpdateId { get; set; }
    [Required]
    public int JobId { get; set; }
    [Required]
    public int FreelancerId { get; set; }
    [Required]
    [Column(TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? UpdateType { get; set; } 
    
    // ✅ ADD THIS PROPERTY
    public UpdateStatus Status { get; set; } = UpdateStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public Job? Job { get; set; }
    [JsonIgnore] public User? Freelancer { get; set; }
}