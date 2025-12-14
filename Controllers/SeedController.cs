using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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

    [HttpPost]
    public async Task<IActionResult> SeedDatabase()
    {
        // 1. Ensure Users Exist
        var client1 = await _context.Users.FirstOrDefaultAsync(u => u.Email == "alice@test.com");
        if (client1 == null) return BadRequest("Please seed users first (run the previous seed logic if needed).");

        // 2. Add KEYWORD-RICH Jobs (Targeting the Onboarding Pills)
        if (!await _context.Jobs.AnyAsync(j => j.Title.Contains("React Native")))
        {
            var smartJobs = new List<Job>
            {
                // --- SOFTWARE & MOBILE ---
                new Job { 
                    ClientId = client1.UserId, 
                    Title = "Senior React Native Developer", 
                    Description = "We need an expert in React Native and Expo to build a delivery app. Must know TypeScript.", 
                    Budget = 3000.00m, 
                    Category = "Mobile Apps", 
                    ExperienceLevel = "Expert", 
                    Status = JobStatus.Open 
                },
                new Job { 
                    ClientId = client1.UserId, 
                    Title = "Backend API with Node.js", 
                    Description = "Looking for a backend dev to build REST APIs using Node.js and PostgreSQL.", 
                    Budget = 1200.00m, 
                    Category = "Software Dev", 
                    ExperienceLevel = "Intermediate", 
                    Status = JobStatus.Open 
                },

                // --- DESIGN ---
                new Job { 
                    ClientId = client1.UserId, 
                    Title = "UI/UX Designer for Fintech App", 
                    Description = "Need a modern design in Figma. Clean aesthetic, dark mode required.", 
                    Budget = 800.00m, 
                    Category = "UI/UX Design", 
                    ExperienceLevel = "Intermediate", 
                    Status = JobStatus.Open 
                },
                new Job { 
                    ClientId = client1.UserId, 
                    Title = "Logo Design for Coffee Brand", 
                    Description = "Minimalist logo needed. Must provide Adobe Illustrator (AI) files.", 
                    Budget = 250.00m, 
                    Category = "Logo Design", 
                    ExperienceLevel = "Beginner", 
                    Status = JobStatus.Open 
                },

                // --- MARKETING ---
                new Job { 
                    ClientId = client1.UserId, 
                    Title = "SEO Specialist for E-commerce", 
                    Description = "Improve our Google ranking. Keyword research and backlinking strategy needed.", 
                    Budget = 500.00m, 
                    Category = "SEO", 
                    ExperienceLevel = "Expert", 
                    Status = JobStatus.Open 
                }
            };

            _context.Jobs.AddRange(smartJobs);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Added Smart Jobs for AI testing!" });
    }
}