using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(int userId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // ✅ 1. Mark Single Notification as Read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notif = await _context.Notifications.FindAsync(id);
        if (notif == null) return NotFound();
        
        notif.IsRead = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ✅ 2. Mark Related Notifications as Read (e.g. When viewing a Job)
    [HttpPost("mark-related")]
    public async Task<IActionResult> MarkRelatedAsRead([FromQuery] int userId, [FromQuery] int type, [FromQuery] int entityId)
    {
        var notifs = await _context.Notifications
            .Where(n => n.UserId == userId && n.Type == (NotificationType)type && n.RelatedEntityId == entityId && !n.IsRead)
            .ToListAsync();

        if (notifs.Any())
        {
            foreach (var n in notifs) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }

    // ✅ 3. Mark All of Type as Read (e.g. When viewing Connections tab)
    [HttpPost("mark-all-type")]
    public async Task<IActionResult> MarkAllTypeAsRead([FromQuery] int userId, [FromQuery] int type)
    {
        var notifs = await _context.Notifications
            .Where(n => n.UserId == userId && n.Type == (NotificationType)type && !n.IsRead)
            .ToListAsync();

        if (notifs.Any())
        {
            foreach (var n in notifs) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }
}