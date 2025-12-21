using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public enum UserType { Freelancer = 0, Client = 1, Admin = 2 }

public class User
{
    [Key] public int UserId { get; set; }

    [Required] [StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(150)] public string? JobTitle { get; set; } // Professional Headline

    [Required] [EmailAddress] [StringLength(255)] public string Email { get; set; } = string.Empty;
    [Required] [StringLength(500)] public string PasswordHash { get; set; } = string.Empty;
    [StringLength(20)] public string? PhoneNumber { get; set; }
    [Required] public UserType UserType { get; set; }

    [Column(TypeName = "TEXT")] public string? ProfileBio { get; set; }
    [Column(TypeName = "LONGTEXT")] public string? ProfileImageUrl { get; set; }
    
    // ✅ NEW: Store CV as Base64 Data URI
    [Column(TypeName = "LONGTEXT")] public string? CvUrl { get; set; }

    // ✅ NEW: LinkedIn Profile URL
    [StringLength(255)] public string? LinkedinUrl { get; set; }

    [StringLength(100)] public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal? HourlyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    [StringLength(500)] public string? Languages { get; set; }

    // We can keep SocialLinks for others, but map LinkedIn to its own column for ease
    [Column(TypeName = "TEXT")] public string? SocialLinks { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal Balance { get; set; } = 0.00m;
    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    [JsonIgnore] public ICollection<Job>? Jobs { get; set; }
    [JsonIgnore] public ICollection<Bid>? Bids { get; set; }
    [JsonIgnore] public ICollection<Review>? ReviewsWritten { get; set; }
    [JsonIgnore] public ICollection<Review>? ReviewsReceived { get; set; }
    [JsonIgnore] public ICollection<Payment>? Payments { get; set; }
    [JsonIgnore] public ICollection<Message>? SentMessages { get; set; }
    [JsonIgnore] public ICollection<Message>? ReceivedMessages { get; set; }
    [JsonIgnore] public ICollection<UserSkill>? UserSkills { get; set; }
    [JsonIgnore] public ICollection<Contract>? ContractsAsFreelancer { get; set; }
    [JsonIgnore] public ICollection<Contract>? ContractsAsClient { get; set; }
    [JsonIgnore] public ICollection<ChatbotLog>? ChatbotLogs { get; set; }
    [JsonIgnore] public ICollection<Notification>? Notifications { get; set; }
}