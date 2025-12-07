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

    // --- 1. GET FEED (Only from Connections) ---
    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        // A. Get List of Friend IDs
        var friendIds = await _context.UserConnections
            .AsNoTracking()
            .Where(c => (c.RequesterId == userId || c.TargetId == userId) && c.Status == "Accepted")
            .Select(c => c.RequesterId == userId ? c.TargetId : c.RequesterId)
            .ToListAsync();

        friendIds.Add(userId); 

        if (!friendIds.Any()) return Ok(new List<object>());

        // B. Fetch Posts
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
                IsLiked = p.Likes != null && p.Likes.Any(l => l.UserId == userId),
                // ✅ FIX: Removed manual null check/ternary. EF Core handles empty collections automatically.
                Comments = p.Comments!.Select(c => new {
                    c.CommentId,
                    c.Content,
                    c.CreatedAt,
                    User = c.User == null ? null : new { c.User.UserId, c.User.FullName, c.User.ProfileImageUrl }
                }).OrderBy(c => c.CreatedAt).ToList()
            })
            .ToListAsync();

        return Ok(posts);
    }

    // --- 2. LIKE POST ---
    [HttpPost("posts/{postId}/like")]
    public async Task<IActionResult> ToggleLike(int postId, [FromQuery] int userId)
    {
        var existing = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existing != null)
        {
            _context.PostLikes.Remove(existing);
        }
        else
        {
            _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    // --- 3. COMMENT ON POST ---
    [HttpPost("posts/comment")]
    public async Task<IActionResult> AddComment([FromBody] PostComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        
        comment.CreatedAt = DateTime.UtcNow;
        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

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

    // --- CONNECTIONS LOGIC ---
    
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] UserConnection req)
    {
        var existing = await _context.UserConnections
            .FirstOrDefaultAsync(c => 
                (c.RequesterId == req.RequesterId && c.TargetId == req.TargetId) ||
                (c.RequesterId == req.TargetId && c.TargetId == req.RequesterId));

        if (existing != null) 
        {
            if (existing.Status == "Pending") return BadRequest("Connection request already pending.");
            if (existing.Status == "Accepted") return BadRequest("You are already connected.");
            return BadRequest("Connection previously rejected.");
        }

        req.Status = "Pending";
        req.CreatedAt = DateTime.UtcNow;
        _context.UserConnections.Add(req);

        var requester = await _context.Users.FindAsync(req.RequesterId);
        var notif = new Notification
        {
            UserId = req.TargetId,
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
            .Where(c => (c.RequesterId == userId || c.TargetId == userId) && c.Status == "Accepted")
            .Include(c => c.Requester)
            .Include(c => c.Target)
            .Select(c => new 
            {
                c.ConnectionId,
                Friend = c.RequesterId == userId ? c.Target : c.Requester,
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
            .Where(c => c.TargetId == userId && c.Status == "Pending")
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
        conn.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(conn);
    }
}

public class UpdateStatusDto { public string Status { get; set; } = string.Empty; }