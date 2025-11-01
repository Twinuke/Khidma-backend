using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class Skill
{
    [Key]
    public int SkillId { get; set; }

    [Required]
    [StringLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    // Navigation properties
    [JsonIgnore]
    public ICollection<UserSkill>? UserSkills { get; set; }
}

