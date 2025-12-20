using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public SeedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("reset-and-seed")]
    public async Task<IActionResult> ResetAndSeed()
    {
        // 1. Wipe all data tables in order to avoid foreign key constraints
        _context.PostLikes.RemoveRange(_context.PostLikes);
        _context.PostComments.RemoveRange(_context.PostComments);
        _context.SocialPosts.RemoveRange(_context.SocialPosts);
        _context.UserConnections.RemoveRange(_context.UserConnections);
        _context.Notifications.RemoveRange(_context.Notifications);
        _context.Contracts.RemoveRange(_context.Contracts);
        _context.Bids.RemoveRange(_context.Bids);
        _context.Jobs.RemoveRange(_context.Jobs);
        _context.UserSkills.RemoveRange(_context.UserSkills);
        _context.Skills.RemoveRange(_context.Skills);
        _context.Users.RemoveRange(_context.Users);
        
        await _context.SaveChangesAsync();

        // 2. Seed Initial Users
        var alice = new User 
        { 
            FullName = "Alice Client", 
            Email = "alice@test.com", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Alice123!"), 
            UserType = UserType.Client,
            Balance = 5000.00m,
            CreatedAt = DateTime.UtcNow
        };

        var bob = new User 
        { 
            FullName = "Bob Freelancer", 
            Email = "bob@test.com", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Bob123!"), 
            UserType = UserType.Freelancer,
            Balance = 0.00m,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        // 3. Seed Skills
        var skills = new List<Skill>
        {
            new Skill { SkillName = "React Native" },
            new Skill { SkillName = "Node.js" },
            new Skill { SkillName = "UI/UX Design" },
            new Skill { SkillName = "SEO" }
        };
        _context.Skills.AddRange(skills);
        await _context.SaveChangesAsync();

        // 4. Seed a Sample Job for Alice
        var job = new Job 
        { 
            ClientId = alice.UserId, 
            Title = "Senior React Native Expert", 
            Description = "Need help building a custom social feed with reactions.", 
            Budget = 2500.00m, 
            Category = "Mobile Dev", 
            ExperienceLevel = "Expert", 
            Status = JobStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // 5. Seed a Social Post for the feed
        var post = new SocialPost 
        { 
            UserId = alice.UserId, 
            Type = PostType.JobPosted,
            JobId = job.JobId,
            JobTitle = job.Title,
            CreatedAt = DateTime.UtcNow 
        };
        _context.SocialPosts.Add(post);
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Database completely reset and seeded!",
            credentials = new[] {
                new { user = "Alice Client", email = "alice@test.com", password = "Alice123!" },
                new { user = "Bob Freelancer", email = "bob@test.com", password = "Bob123!" }
            }
        });
    }
}