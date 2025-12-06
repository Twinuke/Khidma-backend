using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BidsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Bids
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBids()
    {
        return await _context.Bids.AsNoTracking().ToListAsync();
    }

    // GET: api/Bids/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Bid>> GetBid(int id)
    {
        var bid = await _context.Bids.AsNoTracking().FirstOrDefaultAsync(b => b.BidId == id);
        if (bid == null) return NotFound();
        return Ok(bid);
    }

    // GET: api/Bids/job/{jobId}
    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsForJob(int jobId)
    {
        var bids = await _context.Bids.AsNoTracking()
            .Include(b => b.Freelancer) // Include Freelancer info for the Client view
            .Where(b => b.JobId == jobId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(bids);
    }

    // GET: api/Bids/freelancer/{freelancerId}
    // ✅ UPDATED: Now includes Job and Client data for the "My Bids" page
    [HttpGet("freelancer/{freelancerId}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsByFreelancer(int freelancerId)
    {
        var bids = await _context.Bids.AsNoTracking()
            .Include(b => b.Job)
            .ThenInclude(j => j.Client) // Need Client name for the UI
            .Where(b => b.FreelancerId == freelancerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(bids);
    }

    // POST: api/Bids
    [HttpPost]
    public async Task<ActionResult<Bid>> CreateBid([FromBody] Bid bid)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Check if job exists
        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null) return BadRequest("Job not found");

        // ✅ CHECK: Prevent duplicate bids
        var existingBid = await _context.Bids
            .AnyAsync(b => b.JobId == bid.JobId && b.FreelancerId == bid.FreelancerId);
        if (existingBid)
        {
            return BadRequest("You have already placed a bid on this job.");
        }

        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer == null) return BadRequest("Freelancer not found");

        bid.CreatedAt = DateTime.UtcNow;
        bid.Status = BidStatus.Pending;
        _context.Bids.Add(bid);

        // Notification: Bid Placed
        var notif = new Notification
        {
            UserId = bid.FreelancerId,
            Title = "Bid Placed",
            Message = $"You placed a bid of ${bid.BidAmount} on '{job.Title}'.",
            Type = NotificationType.BidPlaced,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBid), new { id = bid.BidId }, bid);
    }

    // PUT: api/Bids/{id}/accept (Keep existing logic)
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptBid(int id)
    {
        var bid = await _context.Bids
            .Include(b => b.Job)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.BidId == id);
        
        if (bid == null) return NotFound();
        if (bid.Status != BidStatus.Pending) return BadRequest("Bid is not pending");

        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null || job.Status != JobStatus.Open) return BadRequest("Job is not open");

        // 1. Update Bid
        bid.Status = BidStatus.Accepted;

        // 2. Create Contract
        var contract = new Contract
        {
            JobId = bid.JobId,
            FreelancerId = bid.FreelancerId,
            ClientId = job.ClientId,
            EscrowAmount = bid.BidAmount,
            StartDate = DateTime.UtcNow,
            Status = ContractStatus.Active
        };
        _context.Contracts.Add(contract);

        // 3. Update Job
        job.Status = JobStatus.Assigned;

        // 4. Update Balance
        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer != null) freelancer.Balance += bid.BidAmount;

        // 5. Notify Freelancer
        var notif = new Notification
        {
            UserId = bid.FreelancerId,
            Title = "Bid Accepted!",
            Message = $"Congrats! Your bid for '{job.Title}' was accepted.",
            Type = NotificationType.BidAccepted,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        // 6. Reject others
        var otherBids = await _context.Bids
            .Where(b => b.JobId == bid.JobId && b.BidId != bid.BidId && b.Status == BidStatus.Pending)
            .ToListAsync();
        foreach (var other in otherBids) other.Status = BidStatus.Rejected;

        await _context.SaveChangesAsync();
        return Ok(contract);
    }
}