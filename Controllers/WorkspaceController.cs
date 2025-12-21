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

    // GET: api/Workspace/job/{jobId} - Get all updates for a job (accessible by both freelancer and client)
    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<object>> GetJobUpdates(int jobId)
    {
        try
        {
            // Verify job exists
            var job = await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

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
            Console.WriteLine($"Get Job Updates Error: {ex.Message}");
            return StatusCode(500, new { error = $"Failed to fetch updates: {ex.Message}" });
        }
    }

    // POST: api/Workspace/update - Create a new job update
    [HttpPost("update")]
    public async Task<ActionResult<object>> CreateUpdate([FromBody] CreateUpdateDto dto)
    {
        try
        {
            if (dto.JobId <= 0 || dto.FreelancerId <= 0)
            {
                return BadRequest(new { error = "Invalid Job ID or Freelancer ID" });
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { error = "Content is required" });
            }

            // Verify job exists
            var job = await _context.Jobs
                .Include(j => j.Client)
                .FirstOrDefaultAsync(j => j.JobId == dto.JobId);

            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Verify freelancer exists
            var freelancer = await _context.Users.FindAsync(dto.FreelancerId);
            if (freelancer == null)
            {
                return NotFound(new { error = "Freelancer not found" });
            }

            // Verify the freelancer is hired for this job
            var bid = await _context.Bids
                .FirstOrDefaultAsync(b => b.JobId == dto.JobId && b.FreelancerId == dto.FreelancerId && b.Status == BidStatus.Accepted);

            if (bid == null)
            {
                return BadRequest(new { error = "You are not assigned to this job" });
            }

            // Create the update
            var update = new JobUpdate
            {
                JobId = dto.JobId,
                FreelancerId = dto.FreelancerId,
                Title = dto.Title,
                Content = dto.Content,
                UpdateType = dto.UpdateType ?? "Update",
                CreatedAt = DateTime.UtcNow
            };

            _context.JobUpdates.Add(update);
            await _context.SaveChangesAsync();

            // Send notification to client
            var notification = new Notification
            {
                UserId = job.ClientId,
                Title = "New Job Update",
                Message = $"{freelancer.FullName} posted an update for '{job.Title}'",
                Type = NotificationType.General,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Return the created update with freelancer info
            var createdUpdate = await _context.JobUpdates
                .AsNoTracking()
                .Include(u => u.Freelancer)
                .Where(u => u.UpdateId == update.UpdateId)
                .Select(u => new
                {
                    u.UpdateId,
                    u.JobId,
                    u.FreelancerId,
                    u.Title,
                    u.Content,
                    u.UpdateType,
                    u.CreatedAt,
                    Freelancer = u.Freelancer == null ? null : new
                    {
                        u.Freelancer.UserId,
                        u.Freelancer.FullName,
                        u.Freelancer.ProfileImageUrl
                    }
                })
                .FirstOrDefaultAsync();

            return Ok(createdUpdate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== CREATE UPDATE ERROR ===");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"===========================");

            return StatusCode(500, new { error = $"Failed to create update: {ex.Message}" });
        }
    }

    // DELETE: api/Workspace/update/{updateId} - Delete an update
    [HttpDelete("update/{updateId}")]
    public async Task<IActionResult> DeleteUpdate(int updateId, [FromQuery] int freelancerId)
    {
        try
        {
            var update = await _context.JobUpdates.FindAsync(updateId);
            if (update == null)
            {
                return NotFound(new { error = "Update not found" });
            }

            if (update.FreelancerId != freelancerId)
            {
                return Forbid("You can only delete your own updates");
            }

            _context.JobUpdates.Remove(update);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Update deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete Update Error: {ex.Message}");
            return StatusCode(500, new { error = $"Failed to delete update: {ex.Message}" });
        }
    }

    // POST: api/Workspace/update/{updateId}/approve - Client approves an update
    [HttpPost("update/{updateId}/approve")]
    public async Task<IActionResult> ApproveUpdate(int updateId, [FromBody] UpdateResponseDto dto)
    {
        try
        {
            var update = await _context.JobUpdates
                .Include(u => u.Job)
                .FirstOrDefaultAsync(u => u.UpdateId == updateId);

            if (update == null)
            {
                return NotFound(new { error = "Update not found" });
            }

            // Verify client owns the job
            if (update.Job?.ClientId != dto.ClientId)
            {
                return Forbid("You can only approve updates for your own jobs");
            }

            // Create approval notification for freelancer
            var freelancer = await _context.Users.FindAsync(update.FreelancerId);
            if (freelancer != null)
            {
                var notification = new Notification
                {
                    UserId = update.FreelancerId,
                    Title = "Update Approved",
                    Message = $"Your update for '{update.Job?.Title ?? "the job"}' has been approved by the client.",
                    Type = NotificationType.General,
                    EntityId = update.JobId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Update approved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Approve Update Error: {ex.Message}");
            return StatusCode(500, new { error = $"Failed to approve update: {ex.Message}" });
        }
    }

    // POST: api/Workspace/update/{updateId}/dismiss - Client dismisses an update
    [HttpPost("update/{updateId}/dismiss")]
    public async Task<IActionResult> DismissUpdate(int updateId, [FromBody] UpdateResponseDto dto)
    {
        try
        {
            var update = await _context.JobUpdates
                .Include(u => u.Job)
                .FirstOrDefaultAsync(u => u.UpdateId == updateId);

            if (update == null)
            {
                return NotFound(new { error = "Update not found" });
            }

            // Verify client owns the job
            if (update.Job?.ClientId != dto.ClientId)
            {
                return Forbid("You can only dismiss updates for your own jobs");
            }

            // Create dismissal notification for freelancer
            var freelancer = await _context.Users.FindAsync(update.FreelancerId);
            if (freelancer != null)
            {
                var notification = new Notification
                {
                    UserId = update.FreelancerId,
                    Title = "Update Dismissed",
                    Message = $"Your update for '{update.Job?.Title ?? "the job"}' was dismissed by the client.",
                    Type = NotificationType.General,
                    EntityId = update.JobId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Update dismissed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dismiss Update Error: {ex.Message}");
            return StatusCode(500, new { error = $"Failed to dismiss update: {ex.Message}" });
        }
    }
}

public class UpdateResponseDto
{
    public int ClientId { get; set; }
    public string? Response { get; set; }
}

public class CreateUpdateDto
{
    public int JobId { get; set; }
    public int FreelancerId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? UpdateType { get; set; }
}

