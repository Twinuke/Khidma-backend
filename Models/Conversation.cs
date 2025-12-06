using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace khidma_backend.Models;

public class Conversation
{
    [Key]
    public int ConversationId { get; set; }

    // The two participants
    public int User1Id { get; set; }
    public int User2Id { get; set; }

    // Optional: Context for the chat (e.g. specific job)
    public int? JobId { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User1 { get; set; }
    public User? User2 { get; set; }
    public Job? Job { get; set; }
    public ICollection<Message>? Messages { get; set; }
}