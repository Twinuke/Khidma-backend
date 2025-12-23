using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkspaceController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkspaceController(AppDbContext context)
    {
        _context = context;
    }

    // 1. GET: Get all updates for a job
    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<object>> GetJobUpdates(int jobId)
    {
        try
        {
            var jobExists = await _context.Jobs.AnyAsync(j => j.JobId == jobId);
            if (!jobExists) return NotFound(new { error = "Job not found" });

            var updates = await _context.JobUpdates
                .AsNoTracking()
                .Include(u => u.Freelancer)
                .Where(u => u.JobId == jobId)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.UpdateId,
                    u.JobId,
                    u.FreelancerId,
                    u.Title,
                    u.Content,
                    u.UpdateType,
                    u.Status, // Returns 0, 1, or 2 to the UI
                    u.CreatedAt,
                    Freelancer = u.Freelancer == null ? null : new
                    {
                        u.Freelancer.UserId,
                        u.Freelancer.FullName,
                        u.Freelancer.ProfileImageUrl
                    }
                })
                .ToListAsync();

            return Ok(updates);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to fetch updates: {ex.Message}" });
        }
    }

    // 2. POST: Create a new job update (Freelancer only)
    [HttpPost("update")]
    public async Task<ActionResult<object>> CreateUpdate([FromBody] CreateUpdateDto dto)
    {
        try
        {
            if (dto.JobId <= 0 || dto.FreelancerId <= 0 || string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { error = "Invalid data" });

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == dto.JobId);
            if (job == null) return NotFound(new { error = "Job not found" });

            // Ensure freelancer is hired
            var isHired = await _context.Bids.AnyAsync(b => b.JobId == dto.JobId && b.FreelancerId == dto.FreelancerId && b.Status == BidStatus.Accepted);
            if (!isHired) return BadRequest(new { error = "Unauthorized: You are not assigned to this job." });

            var update = new JobUpdate
            {
                JobId = dto.JobId,
                FreelancerId = dto.FreelancerId,
                Title = dto.Title,
                Content = dto.Content,
                UpdateType = dto.UpdateType ?? "Update",
                Status = UpdateStatus.Pending, 
                CreatedAt = DateTime.UtcNow
            };

            _context.JobUpdates.Add(update);

            // Notify Client
            _context.Notifications.Add(new Notification
            {
                UserId = job.ClientId,
                Title = "New Job Update",
                Message = $"An update was posted for '{job.Title}'",
                Type = NotificationType.General,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(update);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // 3. POST: Approve an update (Client only)
    [HttpPost("update/{updateId}/approve")]
    public async Task<IActionResult> ApproveUpdate(int updateId, [FromBody] UpdateResponseDto dto)
    {
        try
        {
            var update = await _context.JobUpdates.Include(u => u.Job).FirstOrDefaultAsync(u => u.UpdateId == updateId);
            if (update == null) return NotFound();
            if (update.Job?.ClientId != dto.ClientId) return Forbid();

            // ✅ CRITICAL FIX: Prevent double-processing
            if (update.Status != UpdateStatus.Pending)
            {
                return BadRequest(new { error = "This update has already been processed and cannot be changed." });
            }

            update.Status = UpdateStatus.Approved;

            _context.Notifications.Add(new Notification
            {
                UserId = update.FreelancerId,
                Title = "Update Approved ✅",
                Message = $"Your update for '{update.Job?.Title}' was approved. Reason: {dto.Response ?? "No comment provided"}",
                Type = NotificationType.General,
                EntityId = update.JobId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // 4. POST: Dismiss an update (Client only)
    [HttpPost("update/{updateId}/dismiss")]
    public async Task<IActionResult> DismissUpdate(int updateId, [FromBody] UpdateResponseDto dto)
    {
        try
        {
            var update = await _context.JobUpdates.Include(u => u.Job).FirstOrDefaultAsync(u => u.UpdateId == updateId);
            if (update == null) return NotFound();
            if (update.Job?.ClientId != dto.ClientId) return Forbid();

            // ✅ CRITICAL FIX: Prevent double-processing
            if (update.Status != UpdateStatus.Pending)
            {
                return BadRequest(new { error = "This update has already been processed and cannot be changed." });
            }

            update.Status = UpdateStatus.Dismissed;

            _context.Notifications.Add(new Notification
            {
                UserId = update.FreelancerId,
                Title = "Update Dismissed ❌",
                Message = $"The client requested changes on your update for '{update.Job?.Title}'. Feedback: {dto.Response ?? "Please review requirements."}",
                Type = NotificationType.General,
                EntityId = update.JobId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("update/{updateId}")]
    public async Task<IActionResult> DeleteUpdate(int updateId, [FromQuery] int freelancerId)
    {
        var update = await _context.JobUpdates.FindAsync(updateId);
        if (update == null || update.FreelancerId != freelancerId) return Forbid();
        _context.JobUpdates.Remove(update);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public class UpdateResponseDto { public int ClientId { get; set; } public string? Response { get; set; } }
public class CreateUpdateDto { public int JobId { get; set; } public int FreelancerId { get; set; } public string? Title { get; set; } public string Content { get; set; } = string.Empty; public string? UpdateType { get; set; } }