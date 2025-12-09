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

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

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
            
            // ✅ 2.5 Create Notification for Recipient
            var recipientId = (conv.User1Id == senderId) ? conv.User2Id : conv.User1Id;
            var sender = await _context.Users.FindAsync(senderId);
            
            _context.Notifications.Add(new Notification {
                UserId = recipientId,
                Title = sender?.FullName ?? "New Message",
                Message = content.Length > 50 ? content.Substring(0, 47) + "..." : content,
                Type = NotificationType.ChatMessage, // Assuming Type 4 is ChatMessage in your enum
                RelatedEntityId = conversationId, 
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // 3. Broadcast
        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", msg);
    }
}