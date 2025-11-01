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
    public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
    {
        return await _context.Jobs.AsNoTracking().ToListAsync();
    }

    // GET: api/Jobs/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.JobId == id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    // GET: api/Jobs/by-client/{clientId}
    [HttpGet("by-client/{clientId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByClient(int clientId)
    {
        var jobs = await _context.Jobs.AsNoTracking()
            .Where(j => j.ClientId == clientId)
            .ToListAsync();
        return Ok(jobs);
    }

    // GET: api/Jobs/by-status/{status}
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByStatus(JobStatus status)
    {
        var jobs = await _context.Jobs.AsNoTracking()
            .Where(j => j.Status == status)
            .ToListAsync();
        return Ok(jobs);
    }

    // GET: api/Jobs/by-category/{category}
    [HttpGet("by-category/{category}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByCategory(string category)
    {
        var jobs = await _context.Jobs.AsNoTracking()
            .Where(j => j.Category == category)
            .ToListAsync();
        return Ok(jobs);
    }

    // POST: api/Jobs
    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        // Verify client exists
        var client = await _context.Users.FindAsync(job.ClientId);
        if (client == null) return BadRequest("Client not found");

        job.CreatedAt = DateTime.UtcNow;
        job.Status = JobStatus.Open;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
    }

    // PUT: api/Jobs/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] Job job)
    {
        if (id != job.JobId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingJob = await _context.Jobs.FindAsync(id);
        if (existingJob == null) return NotFound();

        _context.Entry(existingJob).CurrentValues.SetValues(job);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Jobs/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] JobStatus status)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();
        job.Status = status;
        await _context.SaveChangesAsync();
        return NoContent();
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
