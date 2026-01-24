using System.ComponentModel.DataAnnotations;

namespace khidma_backend.Models;

/// <summary>
/// Secure, single-use, time-limited password reset token.
/// Stores only a hash of the token (never store the raw token).
/// </summary>
public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the raw token (Base64Url string).
    /// </summary>
    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


