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
            .Where(b => b.JobId == jobId)
            .ToListAsync();
        return Ok(bids);
    }

    // GET: api/Bids/freelancer/{freelancerId}
    [HttpGet("freelancer/{freelancerId}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsByFreelancer(int freelancerId)
    {
        var bids = await _context.Bids.AsNoTracking()
            .Where(b => b.FreelancerId == freelancerId)
            .ToListAsync();
        return Ok(bids);
    }

    // GET: api/Bids/by-status/{status}
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsByStatus(BidStatus status)
    {
        var bids = await _context.Bids.AsNoTracking()
            .Where(b => b.Status == status)
            .ToListAsync();
        return Ok(bids);
    }

    // POST: api/Bids
    [HttpPost]
    public async Task<ActionResult<Bid>> CreateBid([FromBody] Bid bid)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null) return BadRequest("Job not found");

        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer == null) return BadRequest("Freelancer not found");

        bid.CreatedAt = DateTime.UtcNow;
        bid.Status = BidStatus.Pending;
        _context.Bids.Add(bid);

        // ✅ NOTIFICATION: Bid Placed
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

    // PUT: api/Bids/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBid(int id, [FromBody] Bid bid)
    {
        if (id != bid.BidId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingBid = await _context.Bids.FindAsync(id);
        if (existingBid == null) return NotFound();

        _context.Entry(existingBid).CurrentValues.SetValues(bid);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Bids/{id}/accept
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptBid(int id)
    {
        var bid = await _context.Bids
            .Include(b => b.Job)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.BidId == id);
        
        if (bid == null) return NotFound();
        
        if (bid.Status != BidStatus.Pending)
        {
            return BadRequest("Bid is not in pending status");
        }

        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null) return BadRequest("Job not found");
        if (job.Status != JobStatus.Open)
        {
            return BadRequest("Job is not open for bidding");
        }

        // 1. Update Bid Status
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

        // 3. Update Job Status
        job.Status = JobStatus.Assigned;

        // 4. ✅ UPDATE BALANCE
        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer != null) 
        {
            freelancer.Balance += bid.BidAmount;
        }

        // 5. ✅ NOTIFICATION: Bid Accepted
        var notif = new Notification
        {
            UserId = bid.FreelancerId,
            Title = "Bid Accepted!",
            Message = $"Congrats! Your bid for '{job.Title}' was accepted. Balance updated by ${bid.BidAmount}.",
            Type = NotificationType.BidAccepted,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        // 6. Reject other bids
        var otherBids = await _context.Bids
            .Where(b => b.JobId == bid.JobId && b.BidId != bid.BidId && b.Status == BidStatus.Pending)
            .ToListAsync();
        
        foreach (var otherBid in otherBids)
        {
            otherBid.Status = BidStatus.Rejected;
        }

        await _context.SaveChangesAsync();
        return Ok(contract);
    }

    // PUT: api/Bids/{id}/reject
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectBid(int id)
    {
        var bid = await _context.Bids.FindAsync(id);
        if (bid == null) return NotFound();
        bid.Status = BidStatus.Rejected;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Bids/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBid(int id)
    {
        var bid = await _context.Bids.FindAsync(id);
        if (bid == null) return NotFound();
        _context.Bids.Remove(bid);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}