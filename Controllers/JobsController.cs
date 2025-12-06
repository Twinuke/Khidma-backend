using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _context;

    public JobsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Jobs/search
    [HttpGet("search")]
    public async Task<ActionResult<object>> SearchJobs(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] decimal? minBudget,
        [FromQuery] decimal? maxBudget,
        [FromQuery] bool? isRemote,
        [FromQuery] string? experienceLevel,
        [FromQuery] int? currentUserId, // ✅ NEW: To check if user bid
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var queryable = _context.Jobs
            .Include(j => j.Client)
            .Include(j => j.Bids)
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Open);

        if (!string.IsNullOrWhiteSpace(query))
            queryable = queryable.Where(j => j.Title.Contains(query) || j.Description.Contains(query));

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            queryable = queryable.Where(j => j.Category == category);

        // Pagination & Projection
        var totalCount = await queryable.CountAsync();
        var jobs = await queryable
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new 
            {
                j.JobId,
                j.ClientId,
                j.Title,
                j.Description,
                j.Category,
                j.Budget,
                j.Deadline,
                j.IsRemote,
                j.ExperienceLevel,
                j.CreatedAt,
                Client = new { j.Client.FullName, j.Client.UserId },
                BidsCount = j.Bids.Count,
                // ✅ NEW: Check if THIS user placed a bid
                HasPlacedBid = currentUserId.HasValue && j.Bids.Any(b => b.FreelancerId == currentUserId.Value)
            })
            .ToListAsync();

        return Ok(new
        {
            Data = jobs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    // GET: api/Jobs/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs.Include(j => j.Client).FirstOrDefaultAsync(j => j.JobId == id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    // POST: api/Jobs
    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
    }
}