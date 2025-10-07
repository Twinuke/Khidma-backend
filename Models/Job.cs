using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class Job
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Budget { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Open"; // Open, In Progress, Completed, Cancelled

    public DateTime PostedDate { get; set; }

    // Foreign keys
    public int ClientId { get; set; }
    public User? Client { get; set; }

    // Navigation collections
    public ICollection<Bid>? Bids { get; set; }
    public Payment? Payment { get; set; }
}


