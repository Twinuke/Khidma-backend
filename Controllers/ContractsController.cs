using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContractsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Contracts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contract>>> GetContracts()
    {
        return await _context.Contracts.AsNoTracking().ToListAsync();
    }

    // GET: api/Contracts/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Contract>> GetContract(int id)
    {
        var contract = await _context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.ContractId == id);
        if (contract == null) return NotFound();
        return Ok(contract);
    }

    // GET: api/Contracts/by-job/{jobId}
    [HttpGet("by-job/{jobId}")]
    public async Task<ActionResult<IEnumerable<Contract>>> GetContractsByJob(int jobId)
    {
        var contracts = await _context.Contracts.AsNoTracking()
            .Where(c => c.JobId == jobId)
            .ToListAsync();
        return Ok(contracts);
    }

    // GET: api/Contracts/by-freelancer/{freelancerId}
    [HttpGet("by-freelancer/{freelancerId}")]
    public async Task<ActionResult<IEnumerable<Contract>>> GetContractsByFreelancer(int freelancerId)
    {
        var contracts = await _context.Contracts.AsNoTracking()
            .Where(c => c.FreelancerId == freelancerId)
            .ToListAsync();
        return Ok(contracts);
    }

    // GET: api/Contracts/by-client/{clientId}
    [HttpGet("by-client/{clientId}")]
    public async Task<ActionResult<IEnumerable<Contract>>> GetContractsByClient(int clientId)
    {
        var contracts = await _context.Contracts.AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .ToListAsync();
        return Ok(contracts);
    }

    // GET: api/Contracts/by-status/{status}
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<IEnumerable<Contract>>> GetContractsByStatus(ContractStatus status)
    {
        var contracts = await _context.Contracts.AsNoTracking()
            .Where(c => c.Status == status)
            .ToListAsync();
        return Ok(contracts);
    }

    // POST: api/Contracts
    [HttpPost]
    public async Task<ActionResult<Contract>> CreateContract([FromBody] Contract contract)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify job exists
        var job = await _context.Jobs.FindAsync(contract.JobId);
        if (job == null) return BadRequest("Job not found");

        // Verify freelancer exists
        var freelancer = await _context.Users.FindAsync(contract.FreelancerId);
        if (freelancer == null) return BadRequest("Freelancer not found");

        // Verify client exists
        var client = await _context.Users.FindAsync(contract.ClientId);
        if (client == null) return BadRequest("Client not found");

        contract.StartDate = DateTime.UtcNow;
        contract.Status = ContractStatus.Active;
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContract), new { id = contract.ContractId }, contract);
    }

    // PUT: api/Contracts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContract(int id, [FromBody] Contract contract)
    {
        if (id != contract.ContractId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingContract = await _context.Contracts.FindAsync(id);
        if (existingContract == null) return NotFound();

        _context.Entry(existingContract).CurrentValues.SetValues(contract);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Contracts/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateContractStatus(int id, [FromBody] ContractStatus status)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        contract.Status = status;
        if (status == ContractStatus.Completed)
        {
            contract.EndDate = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/Contracts/{id}/complete
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteContract(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        contract.Status = ContractStatus.Completed;
        contract.EndDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Contracts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContract(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

