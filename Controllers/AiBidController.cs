using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiBidController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiBidController(AppDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public class AiResponse
    {
        public decimal Amount { get; set; }
        public int Days { get; set; }
        public string Proposal { get; set; } = string.Empty;
    }

    [HttpGet("suggest/{jobId}")]
    public async Task<IActionResult> SuggestBid(int jobId)
    {
        // 1. Get Job
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound("Job not found");

        // 2. Get History (Winning bids for similar jobs)
        var history = await _context.Bids
            .Include(b => b.Job)
            .Where(b => b.Job.Category == job.Category && b.Status == BidStatus.Accepted)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new 
            {
                b.BidAmount,
                b.DeliveryTimeDays,
                JobTitle = b.Job.Title,
                JobBudget = b.Job.Budget
            })
            .ToListAsync();

        var systemMessage = "You are an expert freelancer. Analyze the job and writing a winning bid. " +
                            "Return ONLY a JSON object with keys: 'amount' (number), 'days' (number), and 'proposal' (string). " +
                            "Do not include markdown formatting like ```json.";

        var userPrompt = $@"
            Job Details:
            Title: {job.Title}
            Budget: ${job.Budget}
            Description: {job.Description}
            Level: {job.ExperienceLevel}

            Past Winning Bids (Reference):
            {JsonSerializer.Serialize(history)}

            Task:
            1. Suggest a competitive price.
            2. Suggest a realistic delivery time.
            3. Write a short, persuasive proposal.
            
            RESPOND WITH RAW JSON ONLY.";

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
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
            response_format = new { type = "json_object" }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{baseUrl}/chat/completions", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, $"AI Error: {responseString}");

            using var doc = JsonDocument.Parse(responseString);
            var aiContent = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // Clean up potentially messy JSON string
            if (aiContent.StartsWith("```json")) aiContent = aiContent.Replace("```json", "").Replace("```", "");
            
            var suggestion = JsonSerializer.Deserialize<AiResponse>(aiContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            return BadRequest($"AI Failed: {ex.Message}");
        }
    }
}