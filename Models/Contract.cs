using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum ContractStatus
{
    Active = 0,
    Completed = 1,
    Disputed = 2,
    Cancelled = 3
}

public class Contract
{
    [Key]
    public int ContractId { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public int FreelancerId { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Escrow amount must be a positive value")]
    public decimal EscrowAmount { get; set; }

    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    [Required]
    public ContractStatus Status { get; set; } = ContractStatus.Active;

    // Navigation properties
    [JsonIgnore]
    public Job? Job { get; set; }

    [JsonIgnore]
    public User? Freelancer { get; set; }

    [JsonIgnore]
    public User? Client { get; set; }

    [JsonIgnore]
    public ICollection<Payment>? Payments { get; set; }

    [JsonIgnore]
    public ICollection<Review>? Reviews { get; set; }
}

