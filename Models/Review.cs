using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class Review
{
    [Key]
    public int ReviewId { get; set; }

    public int ReviewerId { get; set; }
    
    // ✅ Matches AppDbContext
    public int RevieweeId { get; set; } 
    public int? ContractId { get; set; } 

    [Range(1, 5)]
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ReviewerId")]
    public User? Reviewer { get; set; }

    [ForeignKey("RevieweeId")]
    public User? Reviewee { get; set; }

    [ForeignKey("ContractId")]
    public Contract? Contract { get; set; }
}