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
        var bid = await _context.Bids.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (bid == null) return NotFound();
        return Ok(bid);
    }

    // GET: api/Bids/by-job/{jobId}
    // Lists bids for a specific job
    [HttpGet("by-job/{jobId}")]
    public async Task<ActionResult<IEnumerable<Bid>>> GetBidsForJob(int jobId)
    {
        var bids = await _context.Bids.AsNoTracking().Where(b => b.JobId == jobId).ToListAsync();
        return Ok(bids);
    }

    // POST: api/Bids
    // Creates a new bid
    [HttpPost]
    public async Task<ActionResult<Bid>> CreateBid([FromBody] Bid bid)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        bid.BidDate = DateTime.UtcNow;
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBid), new { id = bid.Id }, bid);
    }

    // PUT: api/Bids/{id}
    // Updates an existing bid
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBid(int id, [FromBody] Bid bid)
    {
        if (id != bid.Id) return BadRequest();
        var exists = await _context.Bids.AnyAsync(b => b.Id == id);
        if (!exists) return NotFound();
        _context.Entry(bid).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Bids/{id}/accept
    // Marks a bid as accepted
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptBid(int id)
    {
        var bid = await _context.Bids.FindAsync(id);
        if (bid == null) return NotFound();
        bid.IsAccepted = true;
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


