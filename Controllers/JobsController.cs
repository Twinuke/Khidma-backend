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

    // GET: api/Jobs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetJobs(
        [FromQuery] string? search, 
        [FromQuery] string? category,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Jobs
            .Include(j => j.Client)
            .AsQueryable();

        // 1. Memory Filter Logic (Prevents 500 Error)
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(j => j.Category == category);
        }

        var dbJobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

        // 2. Smart Search in Memory
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerms = search.ToLower().Split(new[] { ' ', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (searchTerms.Any())
            {
                dbJobs = dbJobs.Where(j => searchTerms.Any(term => 
                    (j.Title?.ToLower().Contains(term) ?? false) || 
                    (j.Description?.ToLower().Contains(term) ?? false) ||
                    (j.Category?.ToLower().Contains(term) ?? false)
                )).ToList();
            }
        }

        // 3. Select Data (✅ ADDED ClientId HERE)
        var finalResults = dbJobs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new 
            {
                j.JobId,
                j.Title,
                j.Description,
                j.Budget,
                j.Category,
                j.ExperienceLevel,
                j.IsRemote,
                j.CreatedAt,
                j.Status,
                // ✅ CRITICAL FIX: Sending ClientId so frontend can navigate
                ClientId = j.ClientId, 
                ClientName = j.Client != null ? j.Client.FullName : "Unknown",
                ClientAvatar = j.Client != null ? j.Client.ProfileImageUrl : null,
                BidsCount = _context.Bids.Count(b => b.JobId == j.JobId)
            })
            .ToList();

        return Ok(finalResults);
    }

    // POST: api/Jobs
    [HttpPost]
    public async Task<ActionResult<Job>> PostJob(Job job)
    {
        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJobs), new { id = job.JobId }, job);
    }
}