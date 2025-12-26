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
        return await _context.Bids
            .Include(b => b.Job)
            .Include(b => b.Freelancer)
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/Bids/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Bid>> GetBid(int id)
    {
        var bid = await _context.Bids
            .Include(b => b.Job)
            .Include(b => b.Freelancer)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BidId == id);

        if (bid == null) return NotFound();
        return Ok(bid);
    }

    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetBidsForJob(int jobId)
    {
        var bids = await _context.Bids
            .AsNoTracking()
            .Include(b => b.Freelancer) 
            .Where(b => b.JobId == jobId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new 
            {
                b.BidId,
                b.FreelancerId,
                FreelancerName = b.Freelancer != null ? b.Freelancer.FullName : "Unknown",
                FreelancerAvatar = b.Freelancer != null ? b.Freelancer.ProfileImageUrl : null,
                BidDate = b.CreatedAt,
                Amount = b.BidAmount,
                Proposal = b.ProposalText, 
                b.Status
            })
            .ToListAsync();
        
        return Ok(bids);
    }

    // GET: api/Bids/freelancer/{freelancerId}
    [HttpGet("freelancer/{freelancerId}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsByFreelancer(int freelancerId)
    {
        var bids = await _context.Bids
            .AsNoTracking()
            .Include(b => b.Job)
            .ThenInclude(j => j.Client) 
            .Where(b => b.FreelancerId == freelancerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(bids);
    }

    // POST: api/Bids
    // ✅ FIX: Added logic to notify the Client (Job Owner) when a bid is placed
    [HttpPost]
    public async Task<ActionResult<Bid>> CreateBid([FromBody] Bid bid)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null) return BadRequest("Job not found");

        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer == null) return BadRequest("Freelancer not found");

        if (freelancer.UserType == UserType.Client)
        {
            return BadRequest("Clients cannot place bids on jobs.");
        }

        var existingBid = await _context.Bids
            .AnyAsync(b => b.JobId == bid.JobId && b.FreelancerId == bid.FreelancerId);
        if (existingBid)
        {
            return BadRequest("You have already placed a bid on this job.");
        }

        bid.CreatedAt = DateTime.UtcNow;
        bid.Status = BidStatus.Pending;
        _context.Bids.Add(bid);

        // 1. Notify the Freelancer
        var freelancerNotif = new Notification
        {
            UserId = bid.FreelancerId,
            Title = "Bid Placed",
            Message = $"You placed a bid of ${bid.BidAmount} on '{job.Title}'.",
            Type = NotificationType.BidPlaced,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(freelancerNotif);

        // 2. Notify the Client (Job Owner)
        var clientNotif = new Notification
        {
            UserId = job.ClientId, // Use ClientId from the Job model
            Title = "New Bid Received",
            Message = $"Hey! You just received a new bid of ${bid.BidAmount} on your job: {job.Title}",
            Type = NotificationType.BidPlaced,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(clientNotif);

        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBid), new { id = bid.BidId }, bid);
    }

    // PUT: api/Bids/{id}/accept
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptBid(int id)
    {
        var bid = await _context.Bids
            .Include(b => b.Job)
            .ThenInclude(j => j.Client)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.BidId == id);
        
        if (bid == null) return NotFound();
        if (bid.Status != BidStatus.Pending) return BadRequest("Bid is not pending");

        var job = await _context.Jobs.FindAsync(bid.JobId);
        if (job == null) return BadRequest("Job not found");

        bid.Status = BidStatus.Accepted;

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

        job.Status = JobStatus.Assigned;

        var freelancer = await _context.Users.FindAsync(bid.FreelancerId);
        if (freelancer != null) freelancer.Balance += bid.BidAmount;

        var notif = new Notification
        {
            UserId = bid.FreelancerId,
            Title = "Bid Accepted!",
            Message = $"Congrats! Your bid for '{job.Title}' was accepted.",
            Type = NotificationType.BidAccepted,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notif);

        var otherBids = await _context.Bids
            .Where(b => b.JobId == bid.JobId && b.BidId != bid.BidId && b.Status == BidStatus.Pending)
            .ToListAsync();
        foreach (var other in otherBids) other.Status = BidStatus.Rejected;

        var post = new SocialPost
        {
            UserId = bid.FreelancerId,
            Type = PostType.BidAccepted,
            JobId = job.JobId,
            JobTitle = job.Title,
            SecondPartyName = bid.Job?.Client?.FullName ?? "Client",
            CreatedAt = DateTime.UtcNow
        };
        _context.SocialPosts.Add(post);

        await _context.SaveChangesAsync();
        return Ok(contract);
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