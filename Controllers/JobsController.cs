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
        var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    // GET: api/Jobs/by-client/{clientId}
    // Lists jobs created by a specific client
    [HttpGet("by-client/{clientId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByClient(int clientId)
    {
        var jobs = await _context.Jobs.AsNoTracking().Where(j => j.ClientId == clientId).ToListAsync();
        return Ok(jobs);
    }

    // GET: api/Jobs/by-freelancer/{freelancerId}
    // Lists jobs where a given freelancer has placed a bid
    [HttpGet("by-freelancer/{freelancerId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByFreelancer(int freelancerId)
    {
        var jobs = await _context.Bids.AsNoTracking()
            .Where(b => b.FreelancerId == freelancerId)
            .Select(b => b.Job!)
            .Distinct()
            .ToListAsync();
        return Ok(jobs);
    }

    // POST: api/Jobs
    // Creates a new job
    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        job.PostedDate = DateTime.UtcNow;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    // PUT: api/Jobs/{id}
    // Updates an existing job
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] Job job)
    {
        if (id != job.Id) return BadRequest();
        var exists = await _context.Jobs.AnyAsync(j => j.Id == id);
        if (!exists) return NotFound();
        _context.Entry(job).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH-like: api/Jobs/{id}/status/{status}
    // Updates the status of a job
    [HttpPut("{id}/status/{status}")]
    public async Task<IActionResult> UpdateJobStatus(int id, string status)
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


