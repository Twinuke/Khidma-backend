using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialController : ControllerBase
{
    private readonly AppDbContext _context;

    public SocialController(AppDbContext context)
    {
        _context = context;
    }

    // --- COMMENTS ---

    [HttpGet("comments/{jobId}")]
    public async Task<ActionResult<IEnumerable<JobComment>>> GetComments(int jobId)
    {
        return await _context.JobComments
            .Include(c => c.User)
            .Where(c => c.JobId == jobId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    [HttpPost("comments")]
    public async Task<IActionResult> PostComment([FromBody] JobComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        
        comment.CreatedAt = DateTime.UtcNow;
        _context.JobComments.Add(comment);
        await _context.SaveChangesAsync();
        
        // Return full object with User for frontend display
        var fullComment = await _context.JobComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);

        return Ok(fullComment);
    }

    // --- CONNECTIONS ---

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] UserConnection req)
    {
        var existing = await _context.UserConnections
            .FirstOrDefaultAsync(c => 
                (c.RequesterId == req.RequesterId && c.TargetId == req.TargetId) ||
                (c.RequesterId == req.TargetId && c.TargetId == req.RequesterId));

        if (existing != null) return BadRequest("Connection already exists");

        req.Status = "Pending";
        req.CreatedAt = DateTime.UtcNow;
        _context.UserConnections.Add(req);
        await _context.SaveChangesAsync();

        return Ok(req);
    }
}