using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChatController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ NEW: Get global total unread count for the Chat Tab Badge
    [HttpGet("unread/count/{userId}")]
    public async Task<ActionResult<int>> GetTotalUnreadCount(int userId)
    {
        var count = await _context.Messages
            .Where(m => m.SenderId != userId && !m.IsRead && 
                       (m.Conversation.User1Id == userId || m.Conversation.User2Id == userId))
            .CountAsync();

        return Ok(count);
    }

    [HttpGet("my/{userId}")]
    public async Task<ActionResult> GetMyConversations(int userId)
    {
        var convs = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastUpdated)
            .Select(c => new 
            {
                c.ConversationId,
                OtherUser = c.User1Id == userId ? 
                    new { c.User2.UserId, c.User2.FullName, c.User2.ProfileImageUrl } : 
                    new { c.User1.UserId, c.User1.FullName, c.User1.ProfileImageUrl },
                LastMessage = c.Messages != null 
                    ? c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault() 
                    : null,
                // Include individual count for the list view
                UnreadCount = c.Messages != null 
                    ? c.Messages.Count(m => m.SenderId != userId && !m.IsRead) 
                    : 0
            })
            .ToListAsync();

        return Ok(convs);
    }

    // ✅ UPDATED: Marking read now updates both Messages and Notifications
    [HttpPut("read/{conversationId}/{userId}")]
    public async Task<IActionResult> MarkConversationAsRead(int conversationId, int userId)
    {
        // 1. Mark Messages as Read
        var unreadMessages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
        }

        // 2. Mark Notification as Read (Optional: keeps Home tab in sync)
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId 
                        && n.RelatedEntityId == conversationId 
                        && n.Type == NotificationType.ChatMessage 
                        && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            foreach (var notif in unreadNotifications)
            {
                notif.IsRead = true;
            }
        }

        if (unreadMessages.Any() || unreadNotifications.Any())
        {
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost("open")]
    public async Task<ActionResult> OpenConversation([FromBody] OpenChatRequest req)
    {
        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c => 
                (c.User1Id == req.User1Id && c.User2Id == req.User2Id) || 
                (c.User1Id == req.User2Id && c.User2Id == req.User1Id));

        if (existing != null) return Ok(existing);

        if (req.JobId == null || req.JobId == 0)
        {
            var isConnected = await _context.UserConnections.AnyAsync(c =>
                ((c.RequesterId == req.User1Id && c.TargetId == req.User2Id) ||
                 (c.RequesterId == req.User2Id && c.TargetId == req.User1Id)) 
                && c.Status == "Accepted");

            if (!isConnected) return BadRequest("Users are not connected");
        }

        var newConv = new Conversation
        {
            User1Id = req.User1Id,
            User2Id = req.User2Id,
            JobId = req.JobId,
            LastUpdated = DateTime.UtcNow
        };
        _context.Conversations.Add(newConv);
        await _context.SaveChangesAsync();

        return Ok(newConv);
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult> GetMessages(int conversationId)
    {
        var msgs = await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
        return Ok(msgs);
    }
    
    [HttpGet("{conversationId}")]
    public async Task<ActionResult<Conversation>> GetConversation(int conversationId)
    {
        var conv = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conv == null) return NotFound();
        return Ok(conv);
    }
}

public class OpenChatRequest
{
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public int? JobId { get; set; }
}