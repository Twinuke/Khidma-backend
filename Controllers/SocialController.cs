using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

public class UpdateStatusDto { public string Status { get; set; } = string.Empty; }

[ApiController]
[Route("api/[controller]")]
public class SocialController : ControllerBase
{
    private readonly AppDbContext _context;

    public SocialController(AppDbContext context)
    {
        _context = context;
    }

    // --- 1. GET FEED ---
    // URL: GET /api/Social/feed/{userId}
    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        try
        {
            // Get IDs of friends (Accepted connections only)
            var friendIds = await _context.UserConnections
                .AsNoTracking()
                .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
                .Select(c => c.RequesterId == userId ? c.ReceiverId : c.RequesterId)
                .ToListAsync();

            // Safety: Initialize if null and add user's own ID
            friendIds ??= new List<int>();
            friendIds.Add(userId); 

            var posts = await _context.SocialPosts
                .AsNoTracking()
                .Where(p => friendIds.Contains(p.UserId))
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments!) 
                    .ThenInclude(c => c.User)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new 
                {
                    p.PostId,
                    p.UserId,
                    User = p.User == null ? null : new { p.User.UserId, p.User.FullName, p.User.ProfileImageUrl },
                    p.Type,
                    p.JobId,
                    p.JobTitle,
                    p.SecondPartyName,
                    p.CreatedAt,
                    LikesCount = p.Likes == null ? 0 : p.Likes.Count,
                    MyReaction = p.Likes != null ? p.Likes.Where(l => l.UserId == userId).Select(l => l.ReactionType).FirstOrDefault() : null,
                    IsLiked = p.Likes != null && p.Likes.Any(l => l.UserId == userId),
                    
                    // ✅ FIXED: Using Enumerable.Empty to prevent CS0173 Build Error and 500 crashes
                    Comments = (p.Comments ?? Enumerable.Empty<PostComment>()).Select(c => new {
                        c.CommentId,
                        c.Content,
                        c.CreatedAt,
                        User = c.User == null ? null : new { c.User.UserId, c.User.FullName, c.User.ProfileImageUrl }
                    }).OrderBy(c => c.CreatedAt).ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }
        catch (Exception ex)
        {
            // Log to console for debugging
            Console.WriteLine($"Social Feed Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // --- 2. REACT/LIKE ---
    [HttpPost("posts/{postId}/react")]
    public async Task<IActionResult> ReactToPost(int postId, [FromQuery] int userId, [FromQuery] string? reaction)
    {
        var existing = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existing != null)
        {
            if (existing.ReactionType == reaction || string.IsNullOrEmpty(reaction))
                _context.PostLikes.Remove(existing); // Toggle off
            else
                existing.ReactionType = reaction; // Update reaction
        }
        else if (!string.IsNullOrEmpty(reaction))
        {
            _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId, ReactionType = reaction });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // --- 3. COMMENT ---
    [HttpPost("posts/comment")]
    public async Task<IActionResult> AddComment([FromBody] PostComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        
        comment.CreatedAt = DateTime.UtcNow;
        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        // Return full object with User info for immediate UI update
        var fullComment = await _context.PostComments
            .Include(c => c.User)
            .Where(c => c.CommentId == comment.CommentId)
            .Select(c => new {
                c.CommentId,
                c.Content,
                c.CreatedAt,
                User = c.User == null ? null : new { c.User.UserId, c.User.FullName, c.User.ProfileImageUrl }
            })
            .FirstOrDefaultAsync();

        return Ok(fullComment);
    }

    // --- 4. CONNECTIONS ---
    [HttpGet("status/{requesterId}/{targetId}")]
    public async Task<ActionResult<object>> GetConnectionStatus(int requesterId, int targetId)
    {
        var conn = await _context.UserConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => 
                (c.RequesterId == requesterId && c.ReceiverId == targetId) ||
                (c.RequesterId == targetId && c.ReceiverId == requesterId));

        if (conn == null) return Ok(new { status = "None" });
        return Ok(new { status = conn.Status.ToString() }); 
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] UserConnection req)
    {
        if (req.RequesterId <= 0 || req.ReceiverId <= 0)
            return BadRequest("Invalid Requester or Receiver ID.");

        var existing = await _context.UserConnections
            .FirstOrDefaultAsync(c => 
                (c.RequesterId == req.RequesterId && c.ReceiverId == req.ReceiverId) ||
                (c.RequesterId == req.ReceiverId && c.ReceiverId == req.RequesterId));

        if (existing != null) 
        {
            if (existing.Status == ConnectionStatus.Pending) return BadRequest("Connection request already pending.");
            if (existing.Status == ConnectionStatus.Accepted) return BadRequest("You are already connected.");
            return BadRequest("Connection previously rejected.");
        }

        req.Status = ConnectionStatus.Pending;
        req.CreatedAt = DateTime.UtcNow;
        _context.UserConnections.Add(req);

        var requester = await _context.Users.FindAsync(req.RequesterId);
        var notif = new Notification
        {
            UserId = req.ReceiverId, 
            Title = "New Connection Request",
            Message = $"{requester?.FullName ?? "A user"} sent you a connection request.",
            Type = NotificationType.ConnectionRequest,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        await _context.SaveChangesAsync();
        return Ok(req);
    }

    [HttpGet("connections/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetConnections(int userId)
    {
        var connections = await _context.UserConnections
            .AsNoTracking()
            .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
            .Include(c => c.Requester)
            .Include(c => c.Receiver)
            .Select(c => new 
            {
                c.ConnectionId,
                Friend = c.RequesterId == userId ? c.Receiver : c.Requester,
                Since = c.CreatedAt
            })
            .ToListAsync();

        return Ok(connections);
    }

    [HttpGet("requests/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingRequests(int userId)
    {
        var requests = await _context.UserConnections
            .AsNoTracking()
            .Where(c => c.ReceiverId == userId && c.Status == ConnectionStatus.Pending)
            .Include(c => c.Requester)
            .Select(c => new 
            {
                c.ConnectionId,
                Requester = c.Requester, 
                SentAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPut("connection/{connectionId}")]
    public async Task<IActionResult> RespondToConnection(int connectionId, [FromBody] UpdateStatusDto dto)
    {
        var conn = await _context.UserConnections.FindAsync(connectionId);
        if (conn == null) return NotFound();

        if (Enum.TryParse<ConnectionStatus>(dto.Status, true, out var newStatus))
        {
            conn.Status = newStatus;
            await _context.SaveChangesAsync();
            return Ok(conn);
        }

        return BadRequest("Invalid status");
    }
}