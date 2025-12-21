using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

// ✅ 1. Define the Status Enum
public enum ConnectionStatus
{
    Pending,
    Accepted,
    Rejected
}

public class UserConnection
{
    [Key]
    public int ConnectionId { get; set; }

    // ✅ 2. Add Requester (Sender)
    public int RequesterId { get; set; }
    
    [ForeignKey("RequesterId")]
    public User? Requester { get; set; }

    // ✅ 3. Add Receiver (The missing part causing the error!)
    public int ReceiverId { get; set; }

    [ForeignKey("ReceiverId")]
    public User? Receiver { get; set; }

    // ✅ 4. Add Status
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}