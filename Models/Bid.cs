using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum BidStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}

public class Bid
{
    [Key]
    public int BidId { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public int FreelancerId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Bid amount must be a positive value")]
    public decimal BidAmount { get; set; }

    [Required]
    [Column(TypeName = "TEXT")]
    public string ProposalText { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public BidStatus Status { get; set; } = BidStatus.Pending;

    // Navigation properties
    [JsonIgnore]
    public Job? Job { get; set; }

    [JsonIgnore]
    public User? Freelancer { get; set; }
}
