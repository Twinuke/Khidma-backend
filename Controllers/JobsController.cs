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

    // 1. GET: api/Jobs (Public Feed)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetJobs(
        [FromQuery] string? search, 
        [FromQuery] string? category,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Jobs.Include(j => j.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(j => j.Category == category);
        }

        var dbJobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var terms = search.ToLower().Split(new[] { ' ', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Any())
            {
                dbJobs = dbJobs.Where(j => terms.Any(term => 
                    (j.Title?.ToLower().Contains(term) ?? false) || 
                    (j.Description?.ToLower().Contains(term) ?? false) ||
                    (j.Category?.ToLower().Contains(term) ?? false)
                )).ToList();
            }
        }

        var finalResults = dbJobs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new 
            {
                j.JobId, j.Title, j.Description, j.Budget, j.Category,
                j.ExperienceLevel, j.IsRemote, j.CreatedAt, j.Status, j.ClientId,
                ClientName = j.Client?.FullName ?? "Unknown",
                ClientAvatar = j.Client?.ProfileImageUrl,
                BidsCount = _context.Bids.Count(b => b.JobId == j.JobId)
            })
            .ToList();

        return Ok(finalResults);
    }

    // ✅ FIXED: Missing generic endpoint for single job details
    // Resolves 404s when JobDetails.tsx loads.
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetJob(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Client)
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null) return NotFound();

        return Ok(new
        {
            job.JobId,
            job.Title,
            job.Description,
            job.Budget,
            job.Category,
            job.ExperienceLevel,
            job.IsRemote,
            job.CreatedAt,
            job.Status,
            ClientId = job.ClientId,
            ClientName = job.Client?.FullName ?? "Unknown",
            ClientAvatar = job.Client?.ProfileImageUrl
        });
    }

    // 2. GET: api/Jobs/client/{clientId} (For "My Jobs" Screen)
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetJobsByClient(int clientId)
    {
        var jobs = await _context.Jobs
            .Where(j => j.ClientId == clientId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new 
            {
                j.JobId, j.Title, j.CreatedAt, j.Status, j.Budget,
                BidsCount = _context.Bids.Count(b => b.JobId == j.JobId)
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // 3. GET: api/Jobs/{id}/bids-full (For Client Job Details)
    [HttpGet("{id}/bids-full")]
    public async Task<ActionResult<object>> GetJobWithBids(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Client)
            .Include(j => j.Bids)
                .ThenInclude(b => b.Freelancer) 
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null) return NotFound();

        return Ok(new
        {
            job.JobId,
            job.Title,
            job.Description,
            job.Budget,
            job.Status,
            job.CreatedAt,
            job.Category,
            job.ExperienceLevel,
            job.IsRemote,
            ClientId = job.ClientId,
            ClientName = job.Client?.FullName,
            
            Bids = job.Bids.Select(b => new 
            {
                b.BidId,
                b.BidAmount,
                b.DeliveryTimeDays,
                b.ProposalText, 
                b.CreatedAt,
                b.Status,
                Freelancer = b.Freelancer == null ? null : new 
                {
                    b.Freelancer.UserId,
                    b.Freelancer.FullName,
                    b.Freelancer.ProfileImageUrl
                }
            }).OrderByDescending(b => b.CreatedAt).ToList()
        });
    }

    // 4. POST: api/Jobs (Create Job)
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