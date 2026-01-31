using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserConnectionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserConnectionsController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ 1. SEND REQUEST
    [HttpPost("send")]
    public async Task<IActionResult> SendRequest([FromBody] UserConnection request)
    {
        try
        {
            if (request.RequesterId == request.ReceiverId)
                return BadRequest("You cannot connect with yourself.");

            // Check if connection already exists
            var existing = await _context.UserConnections
                .FirstOrDefaultAsync(c => 
                    (c.RequesterId == request.RequesterId && c.ReceiverId == request.ReceiverId) ||
                    (c.RequesterId == request.ReceiverId && c.ReceiverId == request.RequesterId));

            if (existing != null)
            {
                if (existing.Status == ConnectionStatus.Pending)
                    return BadRequest("Connection request is already pending.");
                
                if (existing.Status == ConnectionStatus.Accepted)
                    return BadRequest("You are already connected.");

                // If previously rejected, allow re-sending by removing old record
                _context.UserConnections.Remove(existing);
                await _context.SaveChangesAsync();
            }

            var newConnection = new UserConnection
            {
                RequesterId = request.RequesterId,
                ReceiverId = request.ReceiverId,
                Status = ConnectionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.UserConnections.Add(newConnection);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Connection request sent!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 2. GET CONNECTION STATUS (Added for Profile Page)
    [HttpGet("status/{userId}/{targetId}")]
    public async Task<IActionResult> GetConnectionStatus(int userId, int targetId)
    {
        try
        {
            var connection = await _context.UserConnections
                .FirstOrDefaultAsync(c => 
                    (c.RequesterId == userId && c.ReceiverId == targetId) ||
                    (c.RequesterId == targetId && c.ReceiverId == userId));

            if (connection == null) 
                return Ok(new { status = "None" });

            return Ok(new { status = connection.Status.ToString() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 3. GET PENDING REQUESTS
    [HttpGet("pending/{userId}")]
    public async Task<IActionResult> GetPendingRequests(int userId)
    {
        try 
        {
            var requests = await _context.UserConnections
                .Include(c => c.Requester) 
                .Where(c => c.ReceiverId == userId && c.Status == ConnectionStatus.Pending)
                .Select(c => new 
                {
                    c.ConnectionId,
                    c.RequesterId,
                    c.ReceiverId,
                    Status = c.Status.ToString(),
                    c.CreatedAt,
                    Requester = c.Requester == null ? null : new {
                        c.Requester.UserId,
                        c.Requester.FullName,
                        c.Requester.ProfileImageUrl,
                        c.Requester.UserType,
                        c.Requester.City,
                        c.Requester.JobTitle
                    }
                })
                .ToListAsync();

            return Ok(requests);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 4. GET CONNECTIONS
    [HttpGet("connected/{userId}")]
    public async Task<IActionResult> GetConnections(int userId)
    {
        try
        {
            var connections = await _context.UserConnections
                .Include(c => c.Requester)
                .Include(c => c.Receiver)
                .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
                .Select(c => new 
                {
                    c.ConnectionId,
                    c.RequesterId,
                    c.ReceiverId,
                    Status = c.Status.ToString(),
                    Requester = c.Requester == null ? null : new { c.Requester.UserId, c.Requester.FullName, c.Requester.ProfileImageUrl, c.Requester.UserType, c.Requester.City, c.Requester.JobTitle },
                    Receiver = c.Receiver == null ? null : new { c.Receiver.UserId, c.Receiver.FullName, c.Receiver.ProfileImageUrl, c.Receiver.UserType, c.Receiver.City, c.Receiver.JobTitle }
                })
                .ToListAsync();

            return Ok(connections);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 5. ACCEPT REQUEST
    [HttpPost("accept/{connectionId}")]
    public async Task<IActionResult> AcceptRequest(int connectionId)
    {
        try
        {
            var conn = await _context.UserConnections.FindAsync(connectionId);
            if (conn == null) return NotFound("Connection not found");

            conn.Status = ConnectionStatus.Accepted;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Connected!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 6. REJECT REQUEST
    [HttpPost("reject/{connectionId}")]
    public async Task<IActionResult> RejectRequest(int connectionId)
    {
        try
        {
            var conn = await _context.UserConnections.FindAsync(connectionId);
            if (conn == null) return NotFound("Connection not found");

            _context.UserConnections.Remove(conn);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Request ignored" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ✅ 7. PEOPLE YOU MAY KNOW – AI-style matching algorithm
    [HttpGet("people-you-may-know/{userId}")]
    public async Task<IActionResult> GetPeopleYouMayKnow(int userId)
    {
        try
        {
            var currentUser = await _context.Users
                .Include(u => u.UserSkills!)
                .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (currentUser == null) return NotFound("User not found.");

            // Exclude: self + anyone with existing connection (pending or accepted)
            var existingConnectionUserIds = await _context.UserConnections
                .Where(c => c.RequesterId == userId || c.ReceiverId == userId)
                .Select(c => c.RequesterId == userId ? c.ReceiverId : c.RequesterId)
                .Distinct()
                .ToListAsync();
            var excludeIds = new HashSet<int>(existingConnectionUserIds) { userId };

            // Current user data for scoring
            var mySkillIds = currentUser.UserSkills?.Select(us => us.SkillId).ToHashSet() ?? new HashSet<int>();
            var myCity = (currentUser.City ?? "").Trim().ToLowerInvariant();
            var myUserType = currentUser.UserType;

            // Accepted connections only (for "mutual connections" score)
            var friendIds = await _context.UserConnections
                .Where(c => (c.RequesterId == userId || c.ReceiverId == userId) && c.Status == ConnectionStatus.Accepted)
                .Select(c => c.RequesterId == userId ? c.ReceiverId : c.RequesterId)
                .Distinct()
                .ToListAsync();
            var friendSet = new HashSet<int>(friendIds);

            // All candidate users (not excluded), with skills
            var candidates = await _context.Users
                .Include(u => u.UserSkills!)
                .ThenInclude(us => us.Skill)
                .Where(u => !excludeIds.Contains(u.UserId))
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.ProfileImageUrl,
                    u.UserType,
                    u.City,
                    u.JobTitle,
                    SkillIds = u.UserSkills!.Select(us => us.SkillId).ToList(),
                    SkillNames = u.UserSkills!.Select(us => us.Skill!.SkillName).ToList()
                })
                .ToListAsync();

            // For mutual connections: get each candidate's accepted connection user IDs
            var candidateIds = candidates.Select(c => c.UserId).ToList();
            var connectionsOfCandidates = await _context.UserConnections
                .Where(c => c.Status == ConnectionStatus.Accepted &&
                    (candidateIds.Contains(c.RequesterId) || candidateIds.Contains(c.ReceiverId)))
                .Select(c => new { c.RequesterId, c.ReceiverId })
                .ToListAsync();
            var mutualCountByCandidate = new Dictionary<int, int>();
            foreach (var c in connectionsOfCandidates)
            {
                var otherId = candidateIds.Contains(c.RequesterId) ? c.ReceiverId : c.RequesterId;
                var candId = candidateIds.Contains(c.RequesterId) ? c.RequesterId : c.ReceiverId;
                if (friendSet.Contains(otherId))
                {
                    if (!mutualCountByCandidate.ContainsKey(candId)) mutualCountByCandidate[candId] = 0;
                    mutualCountByCandidate[candId]++;
                }
            }

            // Score each candidate (algorithm: shared skills, same city, mutual connections, user type relevance)
            const int weightSharedSkill = 28;
            const int weightSameCity = 22;
            const int weightMutualConnection = 18;
            const int weightComplementary = 16;  // Client sees Freelancer / Freelancer sees Client
            const int weightSameType = 10;

            var scored = new List<(object User, int Score, List<string> Reasons)>();

            foreach (var c in candidates)
            {
                int score = 0;
                var reasons = new List<string>();

                int sharedSkills = c.SkillIds.Intersect(mySkillIds).Count();
                if (sharedSkills > 0)
                {
                    score += sharedSkills * weightSharedSkill;
                    reasons.Add(sharedSkills == 1 ? "1 shared skill" : $"{sharedSkills} shared skills");
                }

                if (!string.IsNullOrEmpty(myCity) && myCity == (c.City ?? "").Trim().ToLowerInvariant())
                {
                    score += weightSameCity;
                    reasons.Add("Same city");
                }

                if (mutualCountByCandidate.TryGetValue(c.UserId, out int mutual) && mutual > 0)
                {
                    score += mutual * weightMutualConnection;
                    reasons.Add(mutual == 1 ? "1 mutual connection" : $"{mutual} mutual connections");
                }

                // User type relevance: show complementary (clients ↔ freelancers) and same type (freelancers ↔ freelancers, clients ↔ clients)
                bool isFreelancer = c.UserType == UserType.Freelancer;
                bool myIsFreelancer = myUserType == UserType.Freelancer;
                if (isFreelancer != myIsFreelancer)
                {
                    score += weightComplementary;
                    reasons.Add(myIsFreelancer ? "Client – may need your skills" : "Freelancer – relevant to your work");
                }
                else if (isFreelancer && myIsFreelancer)
                {
                    score += weightSameType;
                    reasons.Add("Same role – collaborate");
                }
                else
                {
                    score += weightSameType;
                    reasons.Add("Same role – network");
                }

                var userPayload = new
                {
                    c.UserId,
                    c.FullName,
                    c.ProfileImageUrl,
                    c.UserType,
                    c.City,
                    c.JobTitle,
                    SkillNames = c.SkillNames
                };
                scored.Add((userPayload, score, reasons));
            }

            var ordered = scored
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Reasons.Count)
                .Take(50)
                .Select(x => new
                {
                    user = x.User,
                    matchScore = Math.Min(99, x.Score),
                    matchReasons = x.Reasons
                })
                .ToList();

            return Ok(ordered);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}