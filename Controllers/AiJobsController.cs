using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiJobsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AiJobsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("recommended/{userId}")]
    public async Task<IActionResult> GetRecommendedJobs(int userId)
    {
        // 1. Get User Profile
        var profile = await _context.UserAiProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (profile == null) 
        {
            return NotFound("Profile not found. Please complete onboarding.");
        }

        // 2. Parse Profile Tags & Clean them
        var mySkills = SplitTags(profile.SelectedSkills);
        var myDomains = SplitTags(profile.SelectedDomains);
        var myTools = SplitTags(profile.SelectedTools);

        // 3. Fetch Open Jobs
        var allJobs = await _context.Jobs
            .Include(j => j.Client)
            .Where(j => j.Status == JobStatus.Open)
            .ToListAsync();

        // 4. Smart Scoring Algorithm
        var scoredJobs = allJobs.Select(job => 
        {
            int score = 0;
            // Combine all job text and normalize to lowercase
            var jobText = (job.Title + " " + job.Description + " " + job.Category).ToLower();

            // --- CATEGORY MATCHING (High Priority) ---
            // "Mobile Apps" -> matches "Mobile" or "App"
            foreach (var domain in myDomains)
            {
                // Exact match? Huge points
                if (jobText.Contains(domain)) score += 20;
                // Partial match? (e.g. "Mobile" in "Mobile Apps")
                else if (domain.Split(' ').Any(word => jobText.Contains(word) && word.Length > 2)) score += 5;
            }

            // --- SKILL MATCHING ---
            foreach (var skill in mySkills)
            {
                if (jobText.Contains(skill)) score += 10;
            }

            // --- TOOL MATCHING (e.g. React, Figma) ---
            foreach (var tool in myTools)
            {
                if (jobText.Contains(tool)) score += 8;
            }

            return new { Job = job, Score = score };
        })
        .Where(x => x.Score > 0) // Only return jobs with at least some relevance
        .OrderByDescending(x => x.Score)
        .Take(20)
        .Select(x => x.Job)
        .ToList();

        return Ok(scoredJobs);
    }

    // Helper to clean and split CSV tags
    private List<string> SplitTags(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags)) return new List<string>();
        return rawTags.ToLower()
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();
    }
}