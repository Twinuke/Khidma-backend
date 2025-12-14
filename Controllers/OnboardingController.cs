using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _context;

    public OnboardingController(AppDbContext context)
    {
        _context = context;
    }

    public class OnboardingRequest
    {
        public int UserId { get; set; }
        public List<string> Domains { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public List<string> Behavior { get; set; } = new();
        public string Confidence { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> SaveProfile([FromBody] OnboardingRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null) return NotFound("User not found");

        // 1. Construct the Natural Language AI Prompt
        var aiPrompt = $@"
            User is a {request.Confidence} freelancer specialized in {string.Join(", ", request.Domains)}.
            Core skills include {string.Join(", ", request.Skills)}.
            Preferred tools stack: {string.Join(", ", request.Tools)}.
            Work culture fit: {string.Join(", ", request.Behavior)}.
        ";

        // 2. Create or Update Profile
        var existingProfile = await _context.UserAiProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId);
        
        if (existingProfile == null)
        {
            existingProfile = new UserAiProfile
            {
                UserId = request.UserId,
                SelectedDomains = string.Join(",", request.Domains),
                SelectedSkills = string.Join(",", request.Skills),
                SelectedTools = string.Join(",", request.Tools),
                WorkStyle = string.Join(",", request.Behavior),
                ConfidenceLevel = request.Confidence,
                GeneratedAiPrompt = aiPrompt
            };
            _context.UserAiProfiles.Add(existingProfile);
        }
        else
        {
            existingProfile.SelectedDomains = string.Join(",", request.Domains);
            existingProfile.SelectedSkills = string.Join(",", request.Skills);
            existingProfile.SelectedTools = string.Join(",", request.Tools);
            existingProfile.WorkStyle = string.Join(",", request.Behavior);
            existingProfile.ConfidenceLevel = request.Confidence;
            existingProfile.GeneratedAiPrompt = aiPrompt;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "AI Profile Built Successfully", prompt = aiPrompt });
    }
}