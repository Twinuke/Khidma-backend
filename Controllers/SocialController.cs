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

    // --- 1. GET FEED (BULLETPROOF VERSION) ---
    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        try
        {
            // Validate userId
            if (userId <= 0)
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Step 1: Get friend IDs (users with Accepted connections)
            var friendIds = new List<int> { userId }; // Always include self

            try
            {
                var connections = await _context.UserConnections
                    .AsNoTracking()
                    .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) 
                        && c.Status == ConnectionStatus.Accepted)
                    .ToListAsync();

                foreach (var conn in connections)
                {
                    var friendId = conn.RequesterId == userId ? conn.ReceiverId : conn.RequesterId;
                    if (friendId > 0 && !friendIds.Contains(friendId))
                    {
                        friendIds.Add(friendId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading connections: {ex.Message}");
                // Continue with just userId if connections fail
            }

            // Step 2: Load posts for these users
            List<SocialPost> postsEntities = new List<SocialPost>();
            try
            {
                postsEntities = await _context.SocialPosts
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Where(p => friendIds.Contains(p.UserId))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading posts: {ex.Message}");
                return Ok(new List<object>()); // Return empty array on error
            }

            if (postsEntities == null || !postsEntities.Any())
            {
                return Ok(new List<object>());
            }

            // Step 3: Get all post IDs
            var postIds = postsEntities.Select(p => p.PostId).ToList();
            if (!postIds.Any())
            {
                return Ok(new List<object>());
            }

            // Step 4: Load likes
            List<PostLike> allLikes = new List<PostLike>();
            try
            {
                allLikes = await _context.PostLikes
                    .AsNoTracking()
                    .Where(l => postIds.Contains(l.PostId))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading likes: {ex.Message}");
                // Continue with empty likes list
            }

            // Step 5: Load comments with users
            List<PostComment> allCommentsEntities = new List<PostComment>();
            try
            {
                allCommentsEntities = await _context.PostComments
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Where(c => postIds.Contains(c.PostId))
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading comments: {ex.Message}");
                // Continue with empty comments list
            }

            // Step 6: Build result in memory (safe projection)
            var result = new List<object>();
            
            foreach (var p in postsEntities)
            {
                try
                {
                    // Get likes for this post
                    var postLikes = allLikes?.Where(l => l.PostId == p.PostId).ToList() ?? new List<PostLike>();
                    var likesCount = postLikes.Count;
                    var myLike = postLikes.FirstOrDefault(l => l.UserId == userId);
                    
                    // Get comments for this post
                    var postComments = allCommentsEntities?
                        .Where(c => c.PostId == p.PostId)
                        .OrderBy(c => c.CreatedAt)
                        .ToList() ?? new List<PostComment>();

                    var commentList = postComments.Select(c => new
                    {
                        CommentId = c.CommentId,
                        Content = c.Content ?? string.Empty,
                        CreatedAt = c.CreatedAt,
                        User = c.User == null ? null : new
                        {
                            UserId = c.User.UserId,
                            FullName = c.User.FullName ?? string.Empty,
                            ProfileImageUrl = c.User.ProfileImageUrl
                        }
                    }).ToList();

                    var postResult = new
                    {
                        PostId = p.PostId,
                        UserId = p.UserId,
                        User = p.User == null ? null : new
                        {
                            UserId = p.User.UserId,
                            FullName = p.User.FullName ?? string.Empty,
                            ProfileImageUrl = p.User.ProfileImageUrl
                        },
                        Type = (int)p.Type,
                        Content = p.Content,
                        JobId = p.JobId,
                        JobTitle = p.JobTitle ?? string.Empty,
                        SecondPartyName = p.SecondPartyName,
                        CreatedAt = p.CreatedAt,
                        LikesCount = likesCount,
                        MyReaction = myLike?.ReactionType,
                        IsLiked = myLike != null,
                        Comments = commentList
                    };

                    result.Add(postResult);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing post {p.PostId}: {ex.Message}");
                    // Skip this post and continue
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== SOCIAL FEED ERROR ===");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine($"========================");
            
            // Return empty array instead of error to prevent frontend crash
            return Ok(new List<object>());
        }
    }

    // --- 2. REACT/LIKE ---
    [HttpPost("posts/{postId}/react")]
    public async Task<IActionResult> ReactToPost(int postId, [FromQuery] int userId, [FromQuery] string? reaction)
    {
        try
        {
            // Validate parameters
            if (postId <= 0)
            {
                return BadRequest(new { error = "Invalid post ID" });
            }

            if (userId <= 0)
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Verify post exists
            var postExists = await _context.SocialPosts
                .AsNoTracking()
                .AnyAsync(p => p.PostId == postId);

            if (!postExists)
            {
                return NotFound(new { error = "Post not found" });
            }

            // Normalize reaction - treat empty string as null
            var normalizedReaction = string.IsNullOrWhiteSpace(reaction) ? null : reaction;

            // Find existing like/reaction
            var existing = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existing != null)
            {
                // If removing reaction (null or empty) or changing to same reaction, remove it
                if (normalizedReaction == null || existing.ReactionType == normalizedReaction)
                {
                    _context.PostLikes.Remove(existing);
                }
                else
                {
                    // Update reaction type
                    existing.ReactionType = normalizedReaction;
                }
            }
            else if (normalizedReaction != null)
            {
                // Add new reaction only if reaction is provided
                _context.PostLikes.Add(new PostLike 
                { 
                    PostId = postId, 
                    UserId = userId, 
                    ReactionType = normalizedReaction 
                });
            }
            else
            {
                // No reaction and no existing like - nothing to do
                return Ok(new { message = "No action needed", success = true });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== REACT TO POST ERROR ===");
            Console.WriteLine($"PostId: {postId}, UserId: {userId}, Reaction: {reaction}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"===========================");
            
            return StatusCode(500, new { error = $"Failed to process reaction: {ex.Message}" });
        }
    }

    // --- 3. CREATE POST ---
    [HttpPost("posts")]
    public async Task<ActionResult<object>> CreatePost([FromBody] CreatePostDto dto)
    {
        try
        {
            if (dto.UserId <= 0)
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Verify user exists
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Create the post
            var post = new SocialPost
            {
                UserId = dto.UserId,
                Type = PostType.GeneralPost,
                Content = dto.Content ?? string.Empty,
                JobId = null,
                JobTitle = string.Empty,
                SecondPartyName = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.SocialPosts.Add(post);
            await _context.SaveChangesAsync();

            // Get all connections (friends) of the user
            var connections = await _context.UserConnections
                .AsNoTracking()
                .Where(c => (c.RequesterId == dto.UserId || c.ReceiverId == dto.UserId) 
                    && c.Status == ConnectionStatus.Accepted)
                .ToListAsync();

            // Send notifications to all connections
            foreach (var conn in connections)
            {
                var friendId = conn.RequesterId == dto.UserId ? conn.ReceiverId : conn.RequesterId;
                
                var notification = new Notification
                {
                    UserId = friendId,
                    Title = "New Post",
                    Message = $"{user.FullName} just posted something",
                    Type = NotificationType.PostCreated,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // Return the created post with user info
            var createdPost = await _context.SocialPosts
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.PostId == post.PostId)
                .Select(p => new
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    User = p.User == null ? null : new
                    {
                        UserId = p.User.UserId,
                        FullName = p.User.FullName,
                        ProfileImageUrl = p.User.ProfileImageUrl
                    },
                    Type = (int)p.Type,
                    Content = p.Content,
                    JobId = p.JobId,
                    JobTitle = p.JobTitle,
                    SecondPartyName = p.SecondPartyName,
                    CreatedAt = p.CreatedAt,
                    LikesCount = 0,
                    MyReaction = (string?)null,
                    IsLiked = false,
                    Comments = new List<object>()
                })
                .FirstOrDefaultAsync();

            return Ok(createdPost);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== CREATE POST ERROR ===");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"========================");
            
            return StatusCode(500, new { error = $"Failed to create post: {ex.Message}" });
        }
    }

    public class CreatePostDto
    {
        public int UserId { get; set; }
        public string? Content { get; set; }
    }

    // --- 4. COMMENT ---
    [HttpPost("posts/comment")]
    public async Task<IActionResult> AddComment([FromBody] PostComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        
        comment.CreatedAt = DateTime.UtcNow;
        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        var fullComment = await _context.PostComments
            .AsNoTracking()
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
            .Select(c => new 
            {
                c.ConnectionId,
                Friend = c.RequesterId == userId ? 
                    new { c.Receiver!.UserId, c.Receiver.FullName, c.Receiver.ProfileImageUrl } : 
                    new { c.Requester!.UserId, c.Requester.FullName, c.Requester.ProfileImageUrl },
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
            .Select(c => new 
            {
                c.ConnectionId,
                Requester = new { c.Requester!.UserId, c.Requester.FullName, c.Requester.ProfileImageUrl }, 
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