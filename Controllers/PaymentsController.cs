using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace khidma_backend.Controllers;

// DTO to prevent 500 binding errors and handle JSON body correctly
public class StripeRequest
{
    public decimal Amount { get; set; }
    public int UserId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        
        // Pulling key from configuration to avoid GitHub Push Protection triggers
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            // Fallback for local development if not in appsettings
            StripeConfiguration.ApiKey = "sk_test_replace_this_in_appsettings";
        }
        else
        {
            StripeConfiguration.ApiKey = secretKey;
        }
    }

    // ✅ FIXED: Create a Stripe Checkout Session for Demo (Deposit)
    [HttpPost("create-checkout-session")]
    public async Task<ActionResult> CreateCheckoutSession([FromBody] StripeRequest request)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(request.Amount * 100), // Convert to cents
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Khidma Wallet Deposit",
                                Description = $"Funding for User ID: {request.UserId}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                // In a production app, these would point to your mobile app deep links
                SuccessUrl = "https://example.com/success", 
                CancelUrl = "https://example.com/cancel",
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ✅ NEW: Demo Withdrawal Endpoint
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] StripeRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null) return NotFound("User not found");

        if (user.Balance < request.Amount)
            return BadRequest("Insufficient balance for withdrawal");

        // Logic: Deduct from balance
        user.Balance -= request.Amount;
        
        // In a real app, you would use Stripe Payouts API here.
        // For the demo, we just update the database balance.
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Withdrawal successful", newBalance = user.Balance });
    }

    // GET: api/Payments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<khidma_backend.Models.Payment>>> GetPayments()
    {
        return await _context.Payments.AsNoTracking().ToListAsync();
    }

    // GET: api/Payments/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<khidma_backend.Models.Payment>> GetPayment(int id)
    {
        var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentId == id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    // GET: api/Payments/contract/{contractId}
    [HttpGet("contract/{contractId}")]
    public async Task<ActionResult<IEnumerable<khidma_backend.Models.Payment>>> GetPaymentsByContract(int contractId)
    {
        var payments = await _context.Payments.AsNoTracking()
            .Where(p => p.ContractId == contractId)
            .ToListAsync();
        return Ok(payments);
    }

    // GET: api/Payments/by-status/{status}
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<khidma_backend.Models.Payment>>> GetPaymentsByStatus(PaymentStatus status)
    {
        var payments = await _context.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == status)
            .ToListAsync();
        return Ok(payments);
    }

    // POST: api/Payments (Internal record creation)
    [HttpPost]
    public async Task<ActionResult<khidma_backend.Models.Payment>> CreatePayment([FromBody] khidma_backend.Models.Payment payment)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

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
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] khidma_backend.Models.Payment payment)
    {
        if (id != payment.PaymentId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingPayment = await _context.Payments.FindAsync(id);
        if (existingPayment == null) return NotFound();

        _context.Entry(existingPayment).CurrentValues.SetValues(payment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Payments/{id}/status
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