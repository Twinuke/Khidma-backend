using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

// DTOs
public class UserUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileBio { get; set; }
    public string? City { get; set; }
    public string? ProfileImageUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
    public string? Languages { get; set; }
    public string? SocialLinks { get; set; }
    public string? CvUrl { get; set; } 
    public string? LinkedinUrl { get; set; }
}

public class UserProfileDto
{
    public User User { get; set; }
    public int CompletedJobs { get; set; }
    public double AverageRating { get; set; }
    public double SuccessRate { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // GET: api/Users/profile/{id}
    [HttpGet("profile/{id}")]
    public async Task<ActionResult<UserProfileDto>> GetFullProfile(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null) return NotFound();

        // 1. Calculate Completed Jobs
        int completedJobs = 0;
        if (user.UserType == UserType.Freelancer)
        {
            completedJobs = await _context.Contracts.CountAsync(c => c.FreelancerId == id && c.Status == ContractStatus.Completed);
        }
        else
        {
            // For clients, count jobs that reached a serious stage
            completedJobs = await _context.Jobs.CountAsync(j => 
                j.ClientId == id && 
                (j.Status == JobStatus.Completed || j.Status == JobStatus.Assigned || j.Status == JobStatus.InProgress)
            );
        }

        // 2. Calculate Rating (Live from Reviews Table)
        // Note: Using RevieweeId to match your new model
        var reviews = await _context.Reviews
            .Where(r => r.RevieweeId == id)
            .Select(r => r.Rating)
            .ToListAsync();

        double avgRating = reviews.Any() ? reviews.Average() : 0;

        // 3. Success Rate (Simplified logic)
        double successRate = completedJobs > 0 ? 100 : 0; 

        return Ok(new UserProfileDto
        {
            User = user,
            CompletedJobs = completedJobs,
            AverageRating = Math.Round(avgRating, 1),
            SuccessRate = successRate
        });
    }

    // PUT: api/Users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null) return NotFound();

        // Check unique email
        if (dto.Email != existingUser.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists) return BadRequest("Email already exists");
        }

        // Update Fields
        existingUser.FullName = dto.FullName;
        existingUser.JobTitle = dto.JobTitle;
        existingUser.Email = dto.Email;
        existingUser.PhoneNumber = dto.PhoneNumber;
        existingUser.ProfileBio = dto.ProfileBio;
        existingUser.City = dto.City;
        existingUser.ProfileImageUrl = dto.ProfileImageUrl;
        existingUser.Latitude = dto.Latitude;
        existingUser.Longitude = dto.Longitude;
        existingUser.HourlyRate = dto.HourlyRate;
        existingUser.IsAvailable = dto.IsAvailable;
        existingUser.Languages = dto.Languages;
        existingUser.SocialLinks = dto.SocialLinks;
        existingUser.CvUrl = dto.CvUrl;
        existingUser.LinkedinUrl = dto.LinkedinUrl;

        _context.Entry(existingUser).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}