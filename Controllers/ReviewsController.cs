using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Reviews/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetUserReviews(int userId)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Where(r => r.RevieweeId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new 
            {
                r.ReviewId,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                Reviewer = new {
                    r.Reviewer.UserId,
                    r.Reviewer.FullName,
                    r.Reviewer.ProfileImageUrl
                }
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // POST: api/Reviews
    [HttpPost]
    public async Task<ActionResult<Review>> PostReview(Review review)
    {
        if (review.ReviewerId == review.RevieweeId)
            return BadRequest("You cannot rate yourself.");

        review.CreatedAt = DateTime.UtcNow;
        
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserReviews), new { userId = review.RevieweeId }, review);
    }

    // DELETE: api/Reviews/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id, [FromQuery] int userId)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound();

        // ✅ Only allow users to delete reviews on their own profile (where they are the reviewee)
        if (review.RevieweeId != userId)
            return Forbid("You can only delete reviews on your own profile.");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}