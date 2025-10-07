using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class Bid
{
    [Key]
    public int Id { get; set; }

    // FKs
    public int JobId { get; set; }
    public Job? Job { get; set; }

    public int FreelancerId { get; set; }
    public User? Freelancer { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public string ProposalText { get; set; } = string.Empty;

    public DateTime BidDate { get; set; }

    public bool IsAccepted { get; set; }
}


