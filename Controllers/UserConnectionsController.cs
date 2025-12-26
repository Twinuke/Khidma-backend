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
}