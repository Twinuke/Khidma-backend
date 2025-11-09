using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class OtpCode
{
    [Key]
    public int OtpId { get; set; }

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; } = false;
}

