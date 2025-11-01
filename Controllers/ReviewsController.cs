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
        var review = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    // GET: api/Reviews/by-contract/{contractId}
    [HttpGet("by-contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByContract(int contractId)
    {
        var reviews = await _context.Reviews.AsNoTracking()
            .Where(r => r.ContractId == contractId)
            .ToListAsync();
        return Ok(reviews);
    }

    // GET: api/Reviews/by-reviewer/{reviewerId}
    [HttpGet("by-reviewer/{reviewerId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByReviewer(int reviewerId)
    {
        var reviews = await _context.Reviews.AsNoTracking()
            .Where(r => r.ReviewerId == reviewerId)
            .ToListAsync();
        return Ok(reviews);
    }

    // GET: api/Reviews/by-reviewee/{revieweeId}
    [HttpGet("by-reviewee/{revieweeId}")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByReviewee(int revieweeId)
    {
        var reviews = await _context.Reviews.AsNoTracking()
            .Where(r => r.RevieweeId == revieweeId)
            .ToListAsync();
        return Ok(reviews);
    }

    // POST: api/Reviews
    [HttpPost]
    public async Task<ActionResult<Review>> CreateReview([FromBody] Review review)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify contract exists
        var contract = await _context.Contracts.FindAsync(review.ContractId);
        if (contract == null) return BadRequest("Contract not found");

        // Verify reviewer exists
        var reviewer = await _context.Users.FindAsync(review.ReviewerId);
        if (reviewer == null) return BadRequest("Reviewer not found");

        // Verify reviewee exists
        var reviewee = await _context.Users.FindAsync(review.RevieweeId);
        if (reviewee == null) return BadRequest("Reviewee not found");

        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReview), new { id = review.ReviewId }, review);
    }

    // PUT: api/Reviews/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] Review review)
    {
        if (id != review.ReviewId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingReview = await _context.Reviews.FindAsync(id);
        if (existingReview == null) return NotFound();

        _context.Entry(existingReview).CurrentValues.SetValues(review);
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
