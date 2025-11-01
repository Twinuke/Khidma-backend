using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum JobStatus
{
    Open = 0,
    Assigned = 1,
    Completed = 2,
    Cancelled = 3
}

public class Job
{
    [Key]
    public int JobId { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value")]
    public decimal Budget { get; set; }

    public DateTime? Deadline { get; set; }

    [Required]
    public JobStatus Status { get; set; } = JobStatus.Open;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User? Client { get; set; }

    [JsonIgnore]
    public ICollection<Bid>? Bids { get; set; }

    [JsonIgnore]
    public ICollection<Contract>? Contracts { get; set; }

    [JsonIgnore]
    public ICollection<Message>? Messages { get; set; }
}
