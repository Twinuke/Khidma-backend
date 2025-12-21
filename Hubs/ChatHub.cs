using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace khidma_backend.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    
    // ✅ Track online users (UserId -> ConnectionId)
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    // ✅ 1. Handle Connection (User comes online)
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userIdString = httpContext?.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(userIdString))
        {
            OnlineUsers.TryAdd(userIdString, Context.ConnectionId);
            // Notify everyone else that this user is online
            await Clients.Others.SendAsync("UserIsOnline", int.Parse(userIdString));
        }

        await base.OnConnectedAsync();
    }

    // ✅ 2. Handle Disconnect (User goes offline)
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var item = OnlineUsers.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
        if (!string.IsNullOrEmpty(item.Key))
        {
            OnlineUsers.TryRemove(item.Key, out _);
            await Clients.Others.SendAsync("UserIsOffline", int.Parse(item.Key));
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ✅ 3. Get list of currently online users (Called by frontend on connect)
    public async Task<List<int>> GetOnlineUsers()
    {
        return OnlineUsers.Keys.Select(int.Parse).ToList();
    }

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task SendMessage(int conversationId, int senderId, string content)
    {
        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        _context.Messages.Add(msg);

        var conv = await _context.Conversations.FindAsync(conversationId);
        if (conv != null)
        {
            conv.LastUpdated = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", msg);
    }
}