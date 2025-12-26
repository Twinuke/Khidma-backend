using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiJobsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiJobsController(AppDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public class AiJobPostingResponse
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal? Budget { get; set; } // ✅ Nullable to prevent parse errors
        public string? Deadline { get; set; }
        public bool? IsRemote { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? RequiredSkills { get; set; }
        public string? ProjectScope { get; set; }
        public string? Deliverables { get; set; }
        public string? Timeline { get; set; }
        public string? AdditionalDetails { get; set; }
    }

    [HttpPost("suggest-fields")]
    public async Task<IActionResult> SuggestJobFields([FromBody] JsonElement body)
    {
        if (!body.TryGetProperty("userInput", out var userInputProp))
            return BadRequest("userInput is required");

        string userInput = userInputProp.GetString() ?? "";

        var systemMessage = @"You are a professional project manager. Analyze the user's job requirement.
                            If information is missing, use your expertise to provide a logical, professional placeholder.
                            Return ONLY a JSON object with NO markdown formatting.
                            JSON Fields: 
                            'title', 'description', 'category', 'budget' (number only), 'deadline', 'isRemote' (true/false), 
                            'experienceLevel' (Entry, Intermediate, or Expert), 'requiredSkills' (comma separated), 
                            'projectScope', 'deliverables', 'timeline', 'additionalDetails'.";

        var apiKey = _config["AiSettings:ApiKey"];
        var baseUrl = _config["AiSettings:BaseUrl"];
        
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userInput }
            },
            temperature = 0.5,
            response_format = new { type = "json_object" }
        };

        try
        {
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/chat/completions", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return BadRequest("AI Provider currently busy.");

            using var doc = JsonDocument.Parse(responseString);
            var aiContent = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrEmpty(aiContent)) return BadRequest("AI returned no data.");

            // ✅ CLEANING: Remove any markdown backticks if present
            aiContent = Regex.Replace(aiContent, "```json|```", "").Trim();

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString 
            };
            
            var suggestion = JsonSerializer.Deserialize<AiJobPostingResponse>(aiContent, options);
            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            return BadRequest($"Processing Error: {ex.Message}");
        }
    }

    [HttpGet("recommended/{userId}")]
    public async Task<IActionResult> GetRecommendedJobs(int userId)
    {
        var profile = await _context.UserAiProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound("Profile not found.");

        var mySkills = SplitTags(profile.SelectedSkills);
        var myDomains = SplitTags(profile.SelectedDomains);

        // Include the Client data so it's available for projection
        var allJobs = await _context.Jobs
            .Include(j => j.Client)
            .Where(j => j.Status == JobStatus.Open)
            .ToListAsync();

        var scoredJobs = allJobs.Select(job => 
        {
            int score = 0;
            var jobText = (job.Title + " " + job.Description + " " + (job.Category ?? "")).ToLower();
            foreach (var domain in myDomains)
            {
                if (jobText.Contains(domain)) score += 20;
                else if (domain.Split(' ').Any(word => jobText.Contains(word) && word.Length > 2)) score += 5;
            }
            foreach (var skill in mySkills) if (jobText.Contains(skill)) score += 10;
            return new { Job = job, Score = score };
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .Take(20)
        // ✅ FIXED: Project into the anonymous object format expected by JobCard.tsx
        .Select(x => new 
        {
            x.Job.JobId,
            x.Job.Title,
            x.Job.Description,
            x.Job.Budget,
            x.Job.Category,
            x.Job.Location,
            x.Job.ExperienceLevel,
            x.Job.IsRemote,
            x.Job.CreatedAt,
            x.Job.Status,
            x.Job.ClientId,
            ClientName = x.Job.Client?.FullName ?? "Unknown Client",
            ClientAvatar = x.Job.Client?.ProfileImageUrl,
            BidsCount = _context.Bids.Count(b => b.JobId == x.Job.JobId)
        })
        .ToList();

        return Ok(scoredJobs);
    }

    private List<string> SplitTags(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags)) return new List<string>();
        return rawTags.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }
}