using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Messages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
    {
        return await _context.Messages.AsNoTracking().ToListAsync();
    }

    // GET: api/Messages/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetMessage(int id)
    {
        var message = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.MessageId == id);
        if (message == null) return NotFound();
        return Ok(message);
    }

    // GET: api/Messages/by-sender/{senderId}
    [HttpGet("by-sender/{senderId}")]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessagesBySender(int senderId)
    {
        var messages = await _context.Messages.AsNoTracking()
            .Where(m => m.SenderId == senderId)
            .ToListAsync();
        return Ok(messages);
    }

    // GET: api/Messages/by-receiver/{receiverId}
    [HttpGet("by-receiver/{receiverId}")]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessagesByReceiver(int receiverId)
    {
        var messages = await _context.Messages.AsNoTracking()
            .Where(m => m.ReceiverId == receiverId)
            .ToListAsync();
        return Ok(messages);
    }

    // GET: api/Messages/conversation/{senderId}/{receiverId}
    [HttpGet("conversation/{senderId}/{receiverId}")]
    public async Task<ActionResult<IEnumerable<Message>>> GetConversation(int senderId, int receiverId)
    {
        var messages = await _context.Messages.AsNoTracking()
            .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                       (m.SenderId == receiverId && m.ReceiverId == senderId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        return Ok(messages);
    }

    // GET: api/Messages/by-job/{jobId}
    [HttpGet("by-job/{jobId}")]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessagesByJob(int jobId)
    {
        var messages = await _context.Messages.AsNoTracking()
            .Where(m => m.JobId == jobId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        return Ok(messages);
    }

    // POST: api/Messages
    [HttpPost]
    public async Task<ActionResult<Message>> CreateMessage([FromBody] Message message)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify sender exists
        var sender = await _context.Users.FindAsync(message.SenderId);
        if (sender == null) return BadRequest("Sender not found");

        // Verify receiver exists
        var receiver = await _context.Users.FindAsync(message.ReceiverId);
        if (receiver == null) return BadRequest("Receiver not found");

        // Verify job exists if provided
        if (message.JobId.HasValue)
        {
            var job = await _context.Jobs.FindAsync(message.JobId.Value);
            if (job == null) return BadRequest("Job not found");
        }

        message.CreatedAt = DateTime.UtcNow;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMessage), new { id = message.MessageId }, message);
    }

    // PUT: api/Messages/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMessage(int id, [FromBody] Message message)
    {
        if (id != message.MessageId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingMessage = await _context.Messages.FindAsync(id);
        if (existingMessage == null) return NotFound();

        _context.Entry(existingMessage).CurrentValues.SetValues(message);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Messages/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null) return NotFound();
        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

