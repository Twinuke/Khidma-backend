using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace khidma_backend.Models;

public class UserConnection
{
    [Key]
    public int ConnectionId { get; set; }

    public int RequesterId { get; set; }
    public int TargetId { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public User? Requester { get; set; }
    [JsonIgnore] public User? Target { get; set; }
}