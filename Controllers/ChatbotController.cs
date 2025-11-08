using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChatbotController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all chatbot logs for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<ChatbotLog>>> GetChatbotLogsByUser(int userId)
    {
        var logs = await _context.ChatbotLogs
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
        return Ok(logs);
    }

    /// <summary>
    /// Log AI chat interaction (store userId, message, response)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatbotLog>> LogChatInteraction([FromBody] ChatbotLog log)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify user exists
        var user = await _context.Users.FindAsync(log.UserId);
        if (user == null) return BadRequest("User not found");

        log.Timestamp = DateTime.UtcNow;
        _context.ChatbotLogs.Add(log);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetChatbotLogsByUser), new { userId = log.UserId }, log);
    }

    /// <summary>
    /// Get all chatbot logs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChatbotLog>>> GetChatbotLogs()
    {
        return await _context.ChatbotLogs
            .AsNoTracking()
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Get chatbot log by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChatbotLog>> GetChatbotLog(int id)
    {
        var log = await _context.ChatbotLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChatId == id);
        if (log == null) return NotFound();
        return Ok(log);
    }

    /// <summary>
    /// Delete chatbot log
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChatbotLog(int id)
    {
        var log = await _context.ChatbotLogs.FindAsync(id);
        if (log == null) return NotFound();
        _context.ChatbotLogs.Remove(log);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

