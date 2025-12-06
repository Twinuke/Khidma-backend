using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    // Frontend calls this when entering a chat screen
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    // Frontend calls this to send a message
    public async Task SendMessage(int conversationId, int senderId, string content)
    {
        // 1. Save to DB
        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.Messages.Add(msg);

        // 2. Update Conversation timestamp
        var conv = await _context.Conversations.FindAsync(conversationId);
        if (conv != null)
        {
            conv.LastUpdated = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // 3. Broadcast to everyone in this conversation group (Real-time)
        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", msg);
    }
}