using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

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

    [StringLength(100)]
    public string? Title { get; set; } // Optional title for the update

    [StringLength(50)]
    public string? UpdateType { get; set; } // e.g., "Progress", "Milestone", "Question", "Deliverable"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [JsonIgnore]
    public Job? Job { get; set; }

    [JsonIgnore]
    public User? Freelancer { get; set; }
}

