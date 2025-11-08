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
        var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    // GET: api/Payments/contract/{contractId}
    [HttpGet("contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByContract(int contractId)
    {
        var payments = await _context.Payments.AsNoTracking()
            .Where(p => p.ContractId == contractId)
            .ToListAsync();
        return Ok(payments);
    }

    // GET: api/Payments/by-status/{status}
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByStatus(PaymentStatus status)
    {
        var payments = await _context.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == status)
            .ToListAsync();
        return Ok(payments);
    }

    // POST: api/Payments
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify contract exists
        var contract = await _context.Contracts.FindAsync(payment.ContractId);
        if (contract == null) return BadRequest("Contract not found");

        payment.TransactionDate = DateTime.UtcNow;
        payment.PaymentStatus = PaymentStatus.Pending;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
    }

    // PUT: api/Payments/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment payment)
    {
        if (id != payment.PaymentId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingPayment = await _context.Payments.FindAsync(id);
        if (existingPayment == null) return NotFound();

        _context.Entry(existingPayment).CurrentValues.SetValues(payment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Payments/{id}/status - Update payment status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] PaymentStatus status)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();
        payment.PaymentStatus = status;
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
