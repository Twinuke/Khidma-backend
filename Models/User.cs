using System.ComponentModel.DataAnnotations;

namespace khidma_backend.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = string.Empty; // "Freelancer" or "Client"

    public double? Rating { get; set; }

    public DateTime DateCreated { get; set; }

    // Navigation collections
    public ICollection<Job>? JobsPosted { get; set; } // as Client
    public ICollection<Bid>? BidsPlaced { get; set; } // as Freelancer
    public ICollection<Review>? ReviewsGiven { get; set; }
    public ICollection<Review>? ReviewsReceived { get; set; }
}


