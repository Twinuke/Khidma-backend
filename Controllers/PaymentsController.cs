using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Payments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
    {
        return await _context.Payments.AsNoTracking().ToListAsync();
    }

    // GET: api/Payments/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    // POST: api/Payments
    // Creates an escrow (Payment with status Pending/Escrowed)
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        payment.PaymentDate = DateTime.UtcNow;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
    }

    // PUT: api/Payments/{id}
    // Updates a payment record
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment payment)
    {
        if (id != payment.Id) return BadRequest();
        var exists = await _context.Payments.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();
        _context.Entry(payment).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Payments/{id}/release
    // Marks a payment as Released
    [HttpPut("{id}/release")]
    public async Task<IActionResult> ReleasePayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        payment.PaymentStatus = "Released";
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Payments/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}


