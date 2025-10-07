using System.ComponentModel.DataAnnotations;

namespace khidma_backend.Models;

public class Review
{
    [Key]
    public int Id { get; set; }

    // Relationships
    public int ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public int ReviewedUserId { get; set; }
    public User? ReviewedUser { get; set; }

    public int JobId { get; set; }
    public Job? Job { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime ReviewDate { get; set; }
}


