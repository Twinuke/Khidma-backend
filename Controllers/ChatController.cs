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
                    : null
            })
            .ToListAsync();

        return Ok(convs);
    }

    [HttpPost("open")]
    public async Task<ActionResult> OpenConversation([FromBody] OpenChatRequest req)
    {
        // 1. Check if exists
        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c => 
                (c.User1Id == req.User1Id && c.User2Id == req.User2Id) || 
                (c.User1Id == req.User2Id && c.User2Id == req.User1Id));

        if (existing != null) return Ok(existing);

        // 2. ✅ Check Permission: If no JobId, users MUST be connected
        if (req.JobId == null || req.JobId == 0)
        {
            var isConnected = await _context.UserConnections.AnyAsync(c =>
                ((c.RequesterId == req.User1Id && c.TargetId == req.User2Id) ||
                 (c.RequesterId == req.User2Id && c.TargetId == req.User1Id)) 
                && c.Status == "Accepted");

            if (!isConnected) return BadRequest("Users are not connected");
        }

        // 3. Create new
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