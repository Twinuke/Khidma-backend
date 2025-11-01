using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum PaymentMethod
{
    CreditCard = 0,
    PayPal = 1,
    LocalWallet = 2,
    BankTransfer = 3
}

public enum PaymentStatus
{
    Pending = 0,
    Released = 1,
    Refunded = 2
}

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int ContractId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Payment amount must be a positive value")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Contract? Contract { get; set; }
}
