using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class UserSkill
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int SkillId { get; set; }

    // Navigation properties
    [JsonIgnore]
    public User? User { get; set; }

    [JsonIgnore]
    public Skill? Skill { get; set; }
}

