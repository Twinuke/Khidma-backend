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

    // GET: api/Reviews
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
    {
        return await _context.Reviews.AsNoTracking().ToListAsync();
    }

    // GET: api/Reviews/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Review>> GetReview(int id)
    {
        var review = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    // GET: api/Reviews/by-user/{userId}
    // Lists reviews for a user (as reviewed)
    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForUser(int userId)
    {
        var reviews = await _context.Reviews.AsNoTracking().Where(r => r.ReviewedUserId == userId).ToListAsync();
        return Ok(reviews);
    }

    // GET: api/Reviews/by-job/{jobId}
    [HttpGet("by-job/{jobId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForJob(int jobId)
    {
        var reviews = await _context.Reviews.AsNoTracking().Where(r => r.JobId == jobId).ToListAsync();
        return Ok(reviews);
    }

    // POST: api/Reviews
    // Creates a new review
    [HttpPost]
    public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        review.ReviewDate = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
    }

    // PUT: api/Reviews/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] Review review)
    {
        if (id != review.Id) return BadRequest();
        var exists = await _context.Reviews.AnyAsync(r => r.Id == id);
        if (!exists) return NotFound();
        _context.Entry(review).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Reviews/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}


