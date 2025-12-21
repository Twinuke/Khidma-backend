using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public enum JobStatus
{
    Open,
    Assigned,   // ✅ Added to fix 'JobStatus does not contain Assigned' error
    InProgress,
    Completed,
    Cancelled
}

public class Job
{
    [Key]
    public int JobId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Budget { get; set; }

    public string Location { get; set; } = string.Empty; 
    public string Category { get; set; } = string.Empty;

    // ✅ Added missing properties causing build errors
    public string ExperienceLevel { get; set; } = "Intermediate"; 
    public bool IsRemote { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public JobStatus Status { get; set; } = JobStatus.Open;

    // ✅ New detailed fields for comprehensive job posting
    [Column(TypeName = "TEXT")]
    public string? RequiredSkills { get; set; } // Comma-separated skills
    
    [Column(TypeName = "TEXT")]
    public string? ProjectScope { get; set; } // Detailed project scope
    
    [Column(TypeName = "TEXT")]
    public string? Deliverables { get; set; } // Expected deliverables
    
    [Column(TypeName = "TEXT")]
    public string? Timeline { get; set; } // Project timeline
    
    [Column(TypeName = "TEXT")]
    public string? AdditionalDetails { get; set; } // Any additional information
    
    public string? Deadline { get; set; } // Project deadline

    // Relationships
    public int ClientId { get; set; }
    
    [ForeignKey("ClientId")]
    public User? Client { get; set; }
    
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    // ✅ Added missing Contracts collection
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}