using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    // FK to Job
    public int JobId { get; set; }
    public Job? Job { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(32)]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Escrowed, Released

    public DateTime PaymentDate { get; set; }
}


