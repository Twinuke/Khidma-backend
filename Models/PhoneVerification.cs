using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

/// <summary>
/// Model for storing phone verification OTP codes
/// </summary>
public class PhoneVerification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpireAt { get; set; }

    [Required]
    public bool IsVerified { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

