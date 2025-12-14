using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class UserAiProfile
{
    [Key]
    public int ProfileId { get; set; }

    [Required]
    public int UserId { get; set; }

    // We store these as comma-separated strings or JSON for simplicity in this demo
    public string SelectedDomains { get; set; } = string.Empty; // e.g., "Web,Mobile"
    public string SelectedSkills { get; set; } = string.Empty;  // e.g., "React,Node"
    public string SelectedTools { get; set; } = string.Empty;   // e.g., "VS Code,Figma"
    public string WorkStyle { get; set; } = string.Empty;       // e.g., "Startup Vibes, Chill"
    public string ConfidenceLevel { get; set; } = string.Empty; // e.g., "Advanced"

    // 🧠 THE CORE: This is what the AI will read later to find jobs
    [Column(TypeName = "TEXT")]
    public string GeneratedAiPrompt { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public User? User { get; set; }
}