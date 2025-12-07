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

    // 1. ADVANCED SEARCH (For Freelancers)
    // GET: api/Jobs/search
    [HttpGet("search")]
    public async Task<ActionResult<object>> SearchJobs(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] int? currentUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var queryable = _context.Jobs
            .Include(j => j.Client)
            .Include(j => j.Bids)
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Open);

        // Filter by Query (Title or Description)
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(j => j.Title.Contains(query) || j.Description.Contains(query));
        }

        // Filter by Category
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            queryable = queryable.Where(j => j.Category == category);
        }

        // Pagination Info
        var totalCount = await queryable.CountAsync();
        
        // Execute Query with Projection
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
                Client = new { j.Client.FullName, j.Client.UserId, j.Client.ProfileImageUrl }, // Send Avatar
                BidsCount = j.Bids.Count,
                // ✅ Check if the current user has already placed a bid
                HasPlacedBid = currentUserId != null && j.Bids.Any(b => b.FreelancerId == currentUserId)
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

    // 2. GET SINGLE JOB (Basic Info)
    // GET: api/Jobs/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null) return NotFound();
        return Ok(job);
    }

    // 3. GET JOBS BY CLIENT (For "My Jobs" Dashboard)
    // GET: api/Jobs/client/{clientId}
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetJobsByClient(int clientId)
    {
        var jobs = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.ClientId == clientId)
            .Include(j => j.Bids) 
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new 
            {
                j.JobId,
                j.Title,
                j.Status,
                j.CreatedAt,
                j.Budget,
                BidsCount = j.Bids.Count // Important for the dashboard stats
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // 4. GET JOB WITH FULL BIDS (For "Client Job Details" / "Proposals" Tab)
    // GET: api/Jobs/{id}/bids-full
    [HttpGet("{id}/bids-full")]
    public async Task<ActionResult<object>> GetJobWithBids(int id)
    {
        var job = await _context.Jobs
            .AsNoTracking()
            .Include(j => j.Client) // Load Client info
            .Include(j => j.Bids)
                .ThenInclude(b => b.Freelancer) // ✅ CRITICAL: Loads Freelancer Name/Photo for the list
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null) return NotFound();

        return Ok(job);
    }

    // 5. POST NEW JOB
    // POST: api/Jobs
    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        // Validate Client exists
        var client = await _context.Users.FindAsync(job.ClientId);
        if (client == null) return BadRequest("Invalid Client ID");

        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
    }
}