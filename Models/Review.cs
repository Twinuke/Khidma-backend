using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class Review
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    public int ContractId { get; set; }

    [Required]
    public int ReviewerId { get; set; }

    [Required]
    public int RevieweeId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Column(TypeName = "TEXT")]
    public string? Comment { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Contract? Contract { get; set; }

    [JsonIgnore]
    public User? Reviewer { get; set; }

    [JsonIgnore]
    public User? Reviewee { get; set; }
}
