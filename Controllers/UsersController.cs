using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

// DTO for Profile Updates
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

// DTO for fetching full profile with stats
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

    // GET: api/Users/profile/{id} - Get Full Profile with Stats
    [HttpGet("profile/{id}")]
    public async Task<ActionResult<UserProfileDto>> GetFullProfile(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.ReviewsReceived)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null) return NotFound();

        // Calculate Stats
        int completedJobs = 0;
        if (user.UserType == UserType.Freelancer)
        {
            completedJobs = await _context.Contracts.CountAsync(c => c.FreelancerId == id && c.Status == ContractStatus.Completed);
        }
        else
        {
            completedJobs = await _context.Jobs.CountAsync(j => j.ClientId == id && j.Status == JobStatus.Completed);
        }

        double avgRating = 0;
        if (user.ReviewsReceived != null && user.ReviewsReceived.Any())
        {
            avgRating = user.ReviewsReceived.Average(r => r.Rating);
        }

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