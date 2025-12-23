using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace khidma_backend.Controllers;

public class UpdateStatusDto { public string Status { get; set; } = string.Empty; }

[ApiController]
[Route("api/[controller]")]
public class SocialController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public SocialController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // --- 1. GET FEED ---
    [HttpGet("feed/{userId}")]
    public async Task<ActionResult<object>> GetFeed(int userId)
    {
        try
        {
            var friends = await _context.UserConnections
                .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
                .Select(c => c.RequesterId == userId ? c.ReceiverId : c.RequesterId)
                .ToListAsync();
            friends.Add(userId);

            var posts = await _context.SocialPosts
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => friends.Contains(p.UserId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var postIds = posts.Select(p => p.PostId).ToList();
            var likes = await _context.PostLikes.Where(l => postIds.Contains(l.PostId)).ToListAsync();
            var comments = await _context.PostComments.Include(c => c.User).Where(c => postIds.Contains(c.PostId)).ToListAsync();

            var result = posts.Select(p => new {
                p.PostId, 
                p.UserId, 
                p.Content, 
                p.JobId, 
                p.JobTitle, 
                p.ImageUrl,           // Added media
                p.DocumentUrl,       // Added media
                p.DocumentName,      // Added media
                p.CreatedAt, 
                p.Type,
                User = new { p.User!.UserId, p.User.FullName, p.User.ProfileImageUrl },
                LikesCount = likes.Count(l => l.PostId == p.PostId),
                IsLiked = likes.Any(l => l.PostId == p.PostId && l.UserId == userId),
                MyReaction = likes.FirstOrDefault(l => l.PostId == p.PostId && l.UserId == userId)?.ReactionType,
                Comments = comments.Where(c => c.PostId == p.PostId).Select(c => new {
                    c.CommentId, c.Content, c.CreatedAt,
                    User = new { c.User!.UserId, c.User.FullName, c.User.ProfileImageUrl }
                })
            });

            return Ok(result);
        }
        catch { return Ok(new List<object>()); }
    }

    // --- 2. ADD COMMENT ---
    [HttpPost("posts/comment")]
    public async Task<IActionResult> AddComment([FromBody] PostComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content)) return BadRequest("Content required");
        comment.CreatedAt = DateTime.UtcNow;
        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync();

        var fullComment = await _context.PostComments.AsNoTracking().Include(c => c.User)
            .Where(c => c.CommentId == comment.CommentId)
            .Select(c => new { c.CommentId, c.Content, c.CreatedAt, User = new { c.User!.UserId, c.User.FullName, c.User.ProfileImageUrl } })
            .FirstOrDefaultAsync();

        return Ok(fullComment);
    }

    // --- 3. DELETE POST ---
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> DeletePost(int postId, [FromQuery] int userId)
    {
        var post = await _context.SocialPosts.FindAsync(postId);
        if (post == null) return NotFound();
        if (post.UserId != userId) return Forbid();

        // Optional: Delete physical files from server when post is deleted
        if (!string.IsNullOrEmpty(post.ImageUrl)) DeleteFile(post.ImageUrl);
        if (!string.IsNullOrEmpty(post.DocumentUrl)) DeleteFile(post.DocumentUrl);

        _context.SocialPosts.Remove(post);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // --- 4. REACT ---
    [HttpPost("posts/{postId}/react")]
    public async Task<IActionResult> ReactToPost(int postId, [FromQuery] int userId, [FromQuery] string? reaction)
    {
        var existing = await _context.PostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(reaction) || existing.ReactionType == reaction) _context.PostLikes.Remove(existing);
            else existing.ReactionType = reaction;
        }
        else if (!string.IsNullOrEmpty(reaction)) _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId, ReactionType = reaction });
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // --- 5. CREATE POST (Updated with File Uploads) ---
    [HttpPost("posts")]
    public async Task<ActionResult<object>> CreatePost([FromForm] CreatePostDto dto)
    {
        try 
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found");

            var post = new SocialPost 
            { 
                UserId = dto.UserId, 
                Content = dto.Content ?? "", 
                CreatedAt = DateTime.UtcNow, 
                Type = PostType.GeneralPost 
            };

            // Handle Image Upload
            if (dto.Image != null)
            {
                post.ImageUrl = await SaveFile(dto.Image, "uploads/social/images");
            }

            // Handle Document Upload
            if (dto.Document != null)
            {
                post.DocumentUrl = await SaveFile(dto.Document, "uploads/social/docs");
                post.DocumentName = dto.Document.FileName;
            }

            _context.SocialPosts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { 
                post.PostId, 
                post.UserId, 
                post.Content, 
                post.ImageUrl,
                post.DocumentUrl,
                post.DocumentName,
                post.CreatedAt, 
                User = new { user.UserId, user.FullName, user.ProfileImageUrl }, 
                LikesCount = 0, 
                Comments = new List<object>() 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // --- HELPER METHODS FOR FILES ---
    private async Task<string> SaveFile(IFormFile file, string folder)
    {
        var folderPath = Path.Combine(_environment.WebRootPath, folder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/{folder}/{fileName}";
    }

    private void DeleteFile(string relativePath)
    {
        var fullPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    public class CreatePostDto 
    { 
        public int UserId { get; set; } 
        public string? Content { get; set; } 
        public IFormFile? Image { get; set; }
        public IFormFile? Document { get; set; }
    }
}