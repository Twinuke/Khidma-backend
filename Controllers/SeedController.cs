using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Needed for AnyAsync, FirstAsync
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
        // 1. Check specifically if our seed user "Alice" exists
        // (Use AnyAsync to be non-blocking)
        var seedUserExists = await _context.Users.AnyAsync(u => u.Email == "alice@test.com");

        if (!seedUserExists)
        {
            // Only add users if Alice is missing
            var users = new List<User>
            {
                new User { FullName = "Alice Client", Email = "alice@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = UserType.Client, PhoneNumber="111111", City = "Beirut" },
                new User { FullName = "Bob Freelancer", Email = "bob@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = UserType.Freelancer, PhoneNumber="222222", City = "Tripoli" },
                new User { FullName = "Charlie Dev", Email = "charlie@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = UserType.Freelancer, PhoneNumber="333333", City = "Sidon" },
                new User { FullName = "Diana Design", Email = "diana@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), UserType = UserType.Client, PhoneNumber="444444", City = "Jounieh" }
            };
            
            // Check for potential duplicates before adding (e.g. if Bob exists but Alice doesn't)
            foreach (var user in users)
            {
                if (!await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    _context.Users.Add(user);
                }
            }
            await _context.SaveChangesAsync();
        }

        // 2. Retrieve the users needed for job assignment
        var client1 = await _context.Users.FirstAsync(u => u.Email == "alice@test.com");
        var client2 = await _context.Users.FirstAsync(u => u.Email == "diana@test.com");

        // 3. Create Fake Jobs if none exist
        if (!await _context.Jobs.AnyAsync())
        {
            var jobs = new List<Job>
            {
                new Job
                {
                    ClientId = client1.UserId,
                    Title = "Build a React Native App for Delivery",
                    Description = "I need a senior developer to build a delivery app similar to Toters. Must know Google Maps API and Redux.",
                    Budget = 1500.00m,
                    Category = "Development",
                    IsRemote = true,
                    ExperienceLevel = "Expert",
                    Deadline = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Job
                {
                    ClientId = client1.UserId,
                    Title = "Logo Design for Coffee Shop",
                    Description = "Modern and minimalist logo for a new specialty coffee shop in Mar Mikhael.",
                    Budget = 200.00m,
                    Category = "Design",
                    IsRemote = true,
                    ExperienceLevel = "Entry",
                    Deadline = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Job
                {
                    ClientId = client2.UserId,
                    Title = "Fix .NET Core Backend Bugs",
                    Description = "I have some 500 errors in my API when searching. Need someone to debug quickly.",
                    Budget = 100.00m,
                    Category = "Development",
                    IsRemote = true,
                    ExperienceLevel = "Intermediate",
                    Deadline = DateTime.UtcNow.AddDays(2),
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                },
                new Job
                {
                    ClientId = client2.UserId,
                    Title = "Wedding Photographer Needed",
                    Description = "Need a photographer for a wedding in Byblos. 8 hours coverage.",
                    Budget = 800.00m,
                    Category = "Photography",
                    IsRemote = false,
                    ExperienceLevel = "Expert",
                    Deadline = DateTime.UtcNow.AddMonths(1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Job
                {
                    ClientId = client1.UserId,
                    Title = "Social Media Manager",
                    Description = "Manage Instagram and TikTok accounts for a fashion brand. 3 posts per week.",
                    Budget = 400.00m,
                    Category = "Marketing",
                    IsRemote = true,
                    ExperienceLevel = "Intermediate",
                    Deadline = DateTime.UtcNow.AddDays(14),
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };
            _context.Jobs.AddRange(jobs);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Database seeded successfully with Users and Jobs!" });
    }
}