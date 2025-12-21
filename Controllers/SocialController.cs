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
    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        // ✅ FIXED: Using ReceiverId and Enum Status
        var friendIds = await _context.UserConnections
            .AsNoTracking()
            .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
            .Select(c => c.RequesterId == userId ? c.ReceiverId : c.RequesterId)
            .ToListAsync();

        friendIds.Add(userId); // Include own posts

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

        // Return full object with User info
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
    
    // Check Connection Status
    [HttpGet("status/{requesterId}/{targetId}")]
    public async Task<ActionResult<object>> GetConnectionStatus(int requesterId, int targetId)
    {
        // ✅ FIXED: Using ReceiverId
        var conn = await _context.UserConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => 
                (c.RequesterId == requesterId && c.ReceiverId == targetId) ||
                (c.RequesterId == targetId && c.ReceiverId == requesterId));

        if (conn == null) return Ok(new { status = "None" });
        return Ok(new { status = conn.Status.ToString() }); // Convert Enum to string
    }

    // Send Connection Request
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] UserConnection req)
    {
        // ✅ FIXED: Using ReceiverId and Enum
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

        // Notify Target (Receiver)
        var requester = await _context.Users.FindAsync(req.RequesterId);
        var notif = new Notification
        {
            UserId = req.ReceiverId, // ✅ Use ReceiverId
            Title = "New Connection Request",
            Message = $"{requester?.FullName ?? "A user"} sent you a connection request.",
            Type = NotificationType.ConnectionRequest,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        await _context.SaveChangesAsync();
        return Ok(req);
    }

    // Get My Connections (Accepted Friends)
    [HttpGet("connections/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetConnections(int userId)
    {
        var connections = await _context.UserConnections
            .AsNoTracking()
            // ✅ FIXED: Using ReceiverId and Enum
            .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
            .Include(c => c.Requester)
            .Include(c => c.Receiver) // ✅ Changed from Target to Receiver
            .Select(c => new 
            {
                c.ConnectionId,
                Friend = c.RequesterId == userId ? c.Receiver : c.Requester,
                Since = c.CreatedAt
            })
            .ToListAsync();

        return Ok(connections);
    }

    // Get Pending Requests (To Me)
    [HttpGet("requests/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingRequests(int userId)
    {
        var requests = await _context.UserConnections
            .AsNoTracking()
            // ✅ FIXED: Using ReceiverId and Enum
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

    // Accept/Reject Request
    [HttpPut("connection/{connectionId}")]
    public async Task<IActionResult> RespondToConnection(int connectionId, [FromBody] UpdateStatusDto dto)
    {
        var conn = await _context.UserConnections.FindAsync(connectionId);
        if (conn == null) return NotFound();

        // ✅ FIXED: Parse string to Enum
        if (Enum.TryParse<ConnectionStatus>(dto.Status, true, out var newStatus))
        {
            conn.Status = newStatus;
            await _context.SaveChangesAsync();
            return Ok(conn);
        }

        return BadRequest("Invalid status");
    }
}