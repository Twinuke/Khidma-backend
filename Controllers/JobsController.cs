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

    // 1. ADVANCED SEARCH
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

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(j => j.Title.Contains(query) || j.Description.Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            queryable = queryable.Where(j => j.Category == category);
        }

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
                Client = j.Client == null ? null : new { j.Client.FullName, j.Client.UserId, j.Client.ProfileImageUrl },
                BidsCount = j.Bids == null ? 0 : j.Bids.Count,
                HasPlacedBid = currentUserId != null && j.Bids != null && j.Bids.Any(b => b.FreelancerId == currentUserId)
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

    // 2. GET SINGLE JOB
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

    // 3. GET JOBS BY CLIENT
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
                BidsCount = j.Bids == null ? 0 : j.Bids.Count 
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // 4. GET JOB WITH FULL BIDS
    [HttpGet("{id}/bids-full")]
    public async Task<ActionResult<object>> GetJobWithBids(int id)
    {
        var job = await _context.Jobs
            .AsNoTracking()
            .Where(j => j.JobId == id)
            .Select(j => new 
            {
                j.JobId,
                j.ClientId,
                j.Title,
                j.Description,
                j.Category,
                j.Budget,
                j.Status,
                j.CreatedAt,
                j.Deadline,
                j.IsRemote,
                j.ExperienceLevel,
                Client = j.Client == null ? null : new 
                {
                    j.Client.UserId,
                    j.Client.FullName,
                    j.Client.ProfileImageUrl
                },
                // ✅ FIX: Added '!' to assert Bids is not null for EF Core projection
                Bids = j.Bids!.Select(b => new 
                {
                    b.BidId,
                    b.JobId,
                    b.FreelancerId,
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
            })
            .FirstOrDefaultAsync();

        if (job == null) return NotFound();

        return Ok(job);
    }

    // 5. POST NEW JOB
    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var client = await _context.Users.FindAsync(job.ClientId);
        if (client == null) return BadRequest("Invalid Client ID");

        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        
        // Create Social Post
        var post = new SocialPost
        {
            UserId = job.ClientId,
            Type = PostType.JobPosted,
            JobId = job.JobId,
            JobTitle = job.Title,
            CreatedAt = DateTime.UtcNow
        };
        _context.SocialPosts.Add(post);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
    }
}