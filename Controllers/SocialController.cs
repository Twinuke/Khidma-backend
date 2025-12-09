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

    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        var friendIds = await _context.UserConnections
            .AsNoTracking()
            .Where(c => (c.RequesterId == userId || c.TargetId == userId) && c.Status == "Accepted")
            .Select(c => c.RequesterId == userId ? c.TargetId : c.RequesterId)
            .ToListAsync();

        friendIds.Add(userId); 

        if (!friendIds.Any()) return Ok(new List<object>());

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
                
                // ✅ 1. Likes Count (Heart Button)
                LikesCount = p.Likes == null ? 0 : p.Likes.Count,
                IsLiked = p.Likes != null && p.Likes.Any(l => l.UserId == userId),
                
                // ✅ 2. Reactions (Emoji Picker)
                Reactions = _context.PostReactions
                    .Where(r => r.PostId == p.PostId)
                    .GroupBy(r => r.Reaction)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList(),
                
                // ✅ 3. My Current Reaction (for toggle logic)
                MyReaction = _context.PostReactions
                    .Where(r => r.PostId == p.PostId && r.UserId == userId)
                    .Select(r => r.Reaction)
                    .FirstOrDefault(),
                
                Comments = p.Comments!.Select(c => new {
                    c.CommentId,
                    c.Content,
                    c.CreatedAt,
                    User = c.User == null ? null : new { c.User.UserId, c.User.FullName, c.User.ProfileImageUrl },
                    LikesCount = _context.CommentLikes.Count(cl => cl.CommentId == c.CommentId),
                    IsLiked = _context.CommentLikes.Any(cl => cl.CommentId == c.CommentId && cl.UserId == userId)
                }).OrderBy(c => c.CreatedAt).ToList()
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpPost("posts/{postId}/like")]
    public async Task<IActionResult> ToggleLike(int postId, [FromQuery] int userId)
    {
        var post = await _context.SocialPosts.FindAsync(postId);
        if (post == null) return NotFound("Post not found");

        var existing = await _context.PostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existing != null)
        {
            _context.PostLikes.Remove(existing);
        }
        else
        {
            _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
            
            // Notify owner if it's not their own post
            if (post.UserId != userId)
            {
                var liker = await _context.Users.FindAsync(userId);
                _context.Notifications.Add(new Notification {
                    UserId = post.UserId,
                    Title = "New Like",
                    Message = $"{liker?.FullName ?? "Someone"} liked your post.",
                    Type = NotificationType.SocialLike,
                    RelatedEntityId = postId
                });
            }
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("posts/react")]
    public async Task<IActionResult> ReactToPost([FromBody] ReactionDto dto)
    {
        var post = await _context.SocialPosts.FindAsync(dto.PostId);
        if (post == null) return NotFound("Post not found");

        var existing = await _context.PostReactions.FirstOrDefaultAsync(r => r.PostId == dto.PostId && r.UserId == dto.UserId);

        if (existing != null)
        {
            // ✅ FIX: Toggle Logic
            // If the user clicks the SAME reaction again -> Remove it
            if (existing.Reaction == dto.Reaction) 
            {
                _context.PostReactions.Remove(existing);
            }
            else 
            {
                // If different -> Update it
                existing.Reaction = dto.Reaction; 
            }
        }
        else
        {
            // If new -> Add it
            _context.PostReactions.Add(new PostReaction { PostId = dto.PostId, UserId = dto.UserId, Reaction = dto.Reaction });
            
            if (post.UserId != dto.UserId)
            {
                var reactor = await _context.Users.FindAsync(dto.UserId);
                _context.Notifications.Add(new Notification {
                    UserId = post.UserId,
                    Title = "New Reaction",
                    Message = $"{reactor?.FullName ?? "Someone"} reacted {dto.Reaction} to your post.",
                    Type = NotificationType.SocialReaction,
                    RelatedEntityId = dto.PostId
                });
            }
        }
        await _context.SaveChangesAsync();
        return Ok();
    }
    
    [HttpPost("posts/comment")]
    public async Task<IActionResult> AddComment([FromBody] PostComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        
        var post = await _context.SocialPosts.FindAsync(comment.PostId);
        if (post == null) return NotFound("Post not found");

        comment.CreatedAt = DateTime.UtcNow;
        _context.PostComments.Add(comment);
        
        if (post.UserId != comment.UserId)
        {
            var commenter = await _context.Users.FindAsync(comment.UserId);
            _context.Notifications.Add(new Notification {
                UserId = post.UserId,
                Title = "New Comment",
                Message = $"{commenter?.FullName ?? "Someone"} commented: {comment.Content}",
                Type = NotificationType.SocialComment,
                RelatedEntityId = post.PostId
            });
        }

        await _context.SaveChangesAsync();
        
        var fullComment = await _context.PostComments
            .Include(c => c.User)
            .Where(c => c.CommentId == comment.CommentId)
            .Select(c => new {
                c.CommentId, c.Content, c.CreatedAt,
                User = c.User == null ? null : new { c.User.UserId, c.User.FullName, c.User.ProfileImageUrl },
                LikesCount = 0, IsLiked = false
            }).FirstOrDefaultAsync();

        return Ok(fullComment);
    }

    [HttpPost("comments/{commentId}/like")]
    public async Task<IActionResult> LikeComment(int commentId, [FromBody] CommentLikeDto dto)
    {
        var existing = await _context.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == dto.UserId);
        if (existing != null) _context.CommentLikes.Remove(existing);
        else _context.CommentLikes.Add(new CommentLike { CommentId = commentId, UserId = dto.UserId });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] UserConnection req)
    {
        var existing = await _context.UserConnections.FirstOrDefaultAsync(c => 
                (c.RequesterId == req.RequesterId && c.TargetId == req.TargetId) ||
                (c.RequesterId == req.TargetId && c.TargetId == req.RequesterId));

        if (existing != null) return BadRequest(existing.Status == "Pending" ? "Pending" : "Connected/Rejected");

        req.Status = "Pending";
        req.CreatedAt = DateTime.UtcNow;
        _context.UserConnections.Add(req);

        var requester = await _context.Users.FindAsync(req.RequesterId);
        
        _context.Notifications.Add(new Notification {
            UserId = req.TargetId,
            Title = "New Connection Request",
            Message = $"{requester?.FullName ?? "A user"} sent you a connection request.",
            Type = NotificationType.ConnectionRequest,
            RelatedEntityId = req.RequesterId, 
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(req);
    }

    [HttpGet("connections/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetConnections(int userId)
    {
        var connections = await _context.UserConnections.AsNoTracking()
            .Where(c => (c.RequesterId == userId || c.TargetId == userId) && c.Status == "Accepted")
            .Include(c => c.Requester).Include(c => c.Target)
            .Select(c => new { c.ConnectionId, Friend = c.RequesterId == userId ? c.Target : c.Requester, Since = c.CreatedAt })
            .ToListAsync();
        return Ok(connections);
    }

    [HttpGet("requests/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingRequests(int userId)
    {
        var requests = await _context.UserConnections.AsNoTracking()
            .Where(c => c.TargetId == userId && c.Status == "Pending")
            .Include(c => c.Requester)
            .Select(c => new { c.ConnectionId, Requester = c.Requester, SentAt = c.CreatedAt })
            .ToListAsync();
        return Ok(requests);
    }

    [HttpPut("connection/{connectionId}")]
    public async Task<IActionResult> RespondToConnection(int connectionId, [FromBody] UpdateStatusDto dto)
    {
        var conn = await _context.UserConnections.FindAsync(connectionId);
        if (conn == null) return NotFound();
        conn.Status = dto.Status;

        if (dto.Status == "Accepted") 
        {
            // Notify the Requester that their request was accepted
            _context.Notifications.Add(new Notification {
                UserId = conn.RequesterId, // Send to original requester
                Title = "Connection Accepted",
                Message = "Your connection request was accepted.",
                Type = (NotificationType)10, // Type 10 = ConnectionAccepted
                RelatedEntityId = conn.TargetId, // Link to the user who accepted
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(conn);
    }
}

public class UpdateStatusDto { public string Status { get; set; } = string.Empty; }
public class ReactionDto { public int PostId { get; set; } public int UserId { get; set; } public string Reaction { get; set; } = string.Empty; }
public class CommentLikeDto { public int UserId { get; set; } }