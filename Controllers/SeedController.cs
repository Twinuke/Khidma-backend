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
        // 1. Wipe all data tables (ORDER MATTERS!)
        
        // Level 5: Social Interactions
        _context.PostLikes.RemoveRange(_context.PostLikes);
        _context.PostComments.RemoveRange(_context.PostComments);
        _context.Messages.RemoveRange(_context.Messages);

        // Level 4: Social Connections & Posts
        _context.SocialPosts.RemoveRange(_context.SocialPosts);
        _context.UserConnections.RemoveRange(_context.UserConnections); // ✅ Fixed: Delete connections before Users
        _context.Notifications.RemoveRange(_context.Notifications);
        _context.Reviews.RemoveRange(_context.Reviews);

        // Level 3: Job Dependencies
        _context.Conversations.RemoveRange(_context.Conversations);
        _context.Contracts.RemoveRange(_context.Contracts);
        _context.Bids.RemoveRange(_context.Bids);
        
        // Level 2: Core Entities linked to Users
        _context.Jobs.RemoveRange(_context.Jobs);
        _context.UserSkills.RemoveRange(_context.UserSkills);
        
        // Level 1: Independent Entities
        _context.Skills.RemoveRange(_context.Skills);
        
        // Level 0: Users (Root) - Safe to delete now
        _context.Users.RemoveRange(_context.Users);
        
        await _context.SaveChangesAsync();

        // ---------------------------------------------------------
        // 2. SEED NEW DATA
        // ---------------------------------------------------------

        // Create Users
        var alice = new User 
        { 
            FullName = "Alice Client", 
            Email = "alice@test.com", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Alice123!"), 
            UserType = UserType.Client,
            Balance = 5000.00m,
            CreatedAt = DateTime.UtcNow,
            City = "Beirut",
            IsAvailable = true
        };

        var bob = new User 
        { 
            FullName = "Bob Freelancer", 
            Email = "bob@test.com", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Bob123!"), 
            UserType = UserType.Freelancer,
            Balance = 0.00m,
            CreatedAt = DateTime.UtcNow,
            City = "Jounieh",
            JobTitle = "Senior React Developer",
            HourlyRate = 25.00m,
            IsAvailable = true
        };

        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        // Seed Skills
        var skills = new List<Skill>
        {
            new Skill { SkillName = "React Native" },
            new Skill { SkillName = "Node.js" },
            new Skill { SkillName = "UI/UX Design" },
            new Skill { SkillName = "SEO" },
            new Skill { SkillName = "Video Editing" },
            new Skill { SkillName = "Content Writing" }
        };
        _context.Skills.AddRange(skills);
        await _context.SaveChangesAsync();

        // Seed 8 Diverse Jobs for AI Testing
        var jobs = new List<Job>
        {
            // --- DEVELOPMENT ---
            new Job
            {
                Title = "Senior React Native Developer",
                Description = "We need an expert to build a cross-platform mobile app using React Native, Expo, and TypeScript. Experience with Redux and API integration is required.",
                Budget = 4500.00m,
                Category = "Development",
                ExperienceLevel = "Senior",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow
            },
            new Job
            {
                Title = ".NET Core Backend Engineer",
                Description = "Looking for a C# developer to build scalable REST APIs using .NET 8 and Entity Framework. Must understand SQL Server and Azure.",
                Budget = 3200.00m,
                Category = "Development",
                ExperienceLevel = "Mid",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Job
            {
                Title = "Frontend Developer (Vue.js)",
                Description = "Convert our existing landing page into a dynamic Vue.js application. Tailwind CSS experience is preferred.",
                Budget = 2000.00m,
                Category = "Development",
                ExperienceLevel = "Junior",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },

            // --- DESIGN ---
            new Job
            {
                Title = "UI/UX Designer for Fintech App",
                Description = "We need a modern, clean design for a banking app. Proficiency in Figma is a must. You will create wireframes and high-fidelity prototypes.",
                Budget = 1500.00m,
                Category = "Design",
                ExperienceLevel = "Mid",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Job
            {
                Title = "Logo & Brand Identity Designer",
                Description = "Create a logo and brand guidelines for a new coffee shop startup. We need a minimalist aesthetic using Adobe Illustrator.",
                Budget = 500.00m,
                Category = "Design",
                ExperienceLevel = "Junior",
                IsRemote = false,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            },

            // --- MARKETING ---
            new Job
            {
                Title = "SEO Specialist for E-commerce",
                Description = "Optimize our Shopify store for search engines. Keyword research, on-page SEO, and link building strategies required.",
                Budget = 1200.00m,
                Category = "Marketing",
                ExperienceLevel = "Senior",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },

            // --- WRITING ---
            new Job
            {
                Title = "Tech Blog Writer",
                Description = "Write 5 technical articles about Artificial Intelligence and Machine Learning. Must have experience writing for tech audiences.",
                Budget = 300.00m,
                Category = "Writing",
                ExperienceLevel = "Mid",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow
            },

            // --- VIDEO ---
            new Job
            {
                Title = "Video Editor for YouTube",
                Description = "Edit raw footage into engaging 10-minute YouTube videos. Add captions, transitions, and background music (Premiere Pro).",
                Budget = 800.00m,
                Category = "Video",
                ExperienceLevel = "Mid",
                IsRemote = true,
                ClientId = alice.UserId,
                Status = JobStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _context.Jobs.AddRange(jobs);
        await _context.SaveChangesAsync();

        // 5. Seed Social Post (For the first job)
        var post = new SocialPost 
        { 
            UserId = alice.UserId, 
            Type = PostType.JobPosted,
            JobId = jobs[0].JobId,
            JobTitle = jobs[0].Title,
            CreatedAt = DateTime.UtcNow 
        };
        _context.SocialPosts.Add(post);
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = $"Database reset! Added 2 users and {jobs.Count} jobs.",
            credentials = new[] {
                new { user = "Alice Client", email = "alice@test.com", password = "Alice123!" },
                new { user = "Bob Freelancer", email = "bob@test.com", password = "Bob123!" }
            }
        });
    }
}