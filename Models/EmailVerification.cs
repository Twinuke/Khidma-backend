using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

/// <summary>
/// Model for storing email verification OTP codes
/// </summary>
public class EmailVerification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

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


