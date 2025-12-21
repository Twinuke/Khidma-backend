using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

public class JobResponseDto
{
    public int JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public bool IsRemote { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = "Unknown";
    public string? ClientAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BidsCount { get; set; }
    public string Status { get; set; } = "Open"; // Return as string to avoid enum issues
}

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
    public async Task<ActionResult<IEnumerable<JobResponseDto>>> GetJobs(
        [FromQuery] string? search = null, 
        [FromQuery] string? category = null, 
        [FromQuery] string? location = null)
    {
        try 
        {
            var query = _context.Jobs
                .Include(j => j.Client)
                .Include(j => j.Bids)
                .AsNoTracking() // Boost performance
                .AsQueryable();

            // 1. Search Filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(j => j.Title.ToLower().Contains(lowerSearch) || 
                                         j.Description.ToLower().Contains(lowerSearch));
            }

            // 2. Category Filter
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(j => j.Category == category);
            }
            
            // 3. Location Filter
            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(j => j.Location.Contains(location));
            }

            // Execute Query
            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new JobResponseDto
                {
                    JobId = j.JobId,
                    Title = j.Title,
                    Description = j.Description,
                    Budget = j.Budget,
                    Location = j.Location ?? "Remote",
                    Category = j.Category,
                    ExperienceLevel = j.ExperienceLevel,
                    IsRemote = j.IsRemote,
                    ClientId = j.ClientId,
                    ClientName = j.Client != null ? j.Client.FullName : "Unknown Client",
                    ClientAvatar = j.Client != null ? j.Client.ProfileImageUrl : null,
                    CreatedAt = j.CreatedAt,
                    BidsCount = j.Bids.Count,
                    Status = j.Status.ToString()
                })
                .ToListAsync();

            return Ok(jobs);
        }
        catch (Exception ex)
        {
            // Log the error server-side so you can see it in the terminal
            Console.WriteLine($"Error fetching jobs: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }

    // GET: api/Jobs/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<JobResponseDto>> GetJob(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Client)
            .Include(j => j.Bids)
            .AsNoTracking()
            .Where(j => j.JobId == id)
            .Select(j => new JobResponseDto
            {
                JobId = j.JobId,
                Title = j.Title,
                Description = j.Description,
                Budget = j.Budget,
                Location = j.Location,
                Category = j.Category,
                ExperienceLevel = j.ExperienceLevel,
                IsRemote = j.IsRemote,
                ClientId = j.ClientId,
                ClientName = j.Client != null ? j.Client.FullName : "Unknown Client",
                ClientAvatar = j.Client != null ? j.Client.ProfileImageUrl : null,
                CreatedAt = j.CreatedAt,
                BidsCount = j.Bids.Count,
                Status = j.Status.ToString()
            })
            .FirstOrDefaultAsync();

        if (job == null) return NotFound();

        return Ok(job);
    }

    // POST: api/Jobs
    [HttpPost]
    public async Task<ActionResult<Job>> PostJob(Job job)
    {
        if (job == null) return BadRequest("Job data is null");

        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        
        // Ensure defaults to prevent db errors
        if(string.IsNullOrEmpty(job.Location)) job.Location = "Remote";
        if(string.IsNullOrEmpty(job.Category)) job.Category = "General";

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
    }

    // DELETE: api/Jobs/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}