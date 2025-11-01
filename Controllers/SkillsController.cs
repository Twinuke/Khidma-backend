using khidma_backend.Data;
using khidma_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SkillsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        return await _context.Skills.AsNoTracking().ToListAsync();
    }

    // GET: api/Skills/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var skill = await _context.Skills.AsNoTracking().FirstOrDefaultAsync(s => s.SkillId == id);
        if (skill == null) return NotFound();
        return Ok(skill);
    }

    // GET: api/Skills/by-name/{skillName}
    [HttpGet("by-name/{skillName}")]
    public async Task<ActionResult<Skill>> GetSkillByName(string skillName)
    {
        var skill = await _context.Skills.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SkillName == skillName);
        if (skill == null) return NotFound();
        return Ok(skill);
    }

    // POST: api/Skills
    [HttpPost]
    public async Task<ActionResult<Skill>> CreateSkill([FromBody] Skill skill)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Check if skill name already exists
        var existingSkill = await _context.Skills
            .FirstOrDefaultAsync(s => s.SkillName == skill.SkillName);
        if (existingSkill != null)
        {
            return BadRequest("Skill name already exists");
        }

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSkill), new { id = skill.SkillId }, skill);
    }

    // PUT: api/Skills/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(int id, [FromBody] Skill skill)
    {
        if (id != skill.SkillId) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingSkill = await _context.Skills.FindAsync(id);
        if (existingSkill == null) return NotFound();

        // Check if skill name is being changed and if it already exists
        if (skill.SkillName != existingSkill.SkillName)
        {
            var nameExists = await _context.Skills.AnyAsync(s => s.SkillName == skill.SkillName);
            if (nameExists) return BadRequest("Skill name already exists");
        }

        _context.Entry(existingSkill).CurrentValues.SetValues(skill);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Skills/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null) return NotFound();
        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET: api/Skills/by-user/{userId}
    [HttpGet("by-user/{userId}")]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkillsByUser(int userId)
    {
        var skills = await _context.UserSkills
            .Where(us => us.UserId == userId)
            .Select(us => us.Skill!)
            .AsNoTracking()
            .ToListAsync();
        return Ok(skills);
    }

    // POST: api/Skills/add-to-user
    [HttpPost("add-to-user")]
    public async Task<ActionResult> AddSkillToUser([FromBody] UserSkill userSkill)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Verify user exists
        var user = await _context.Users.FindAsync(userSkill.UserId);
        if (user == null) return BadRequest("User not found");

        // Verify skill exists
        var skill = await _context.Skills.FindAsync(userSkill.SkillId);
        if (skill == null) return BadRequest("Skill not found");

        // Check if user already has this skill
        var existing = await _context.UserSkills
            .FirstOrDefaultAsync(us => us.UserId == userSkill.UserId && us.SkillId == userSkill.SkillId);
        if (existing != null)
        {
            return BadRequest("User already has this skill");
        }

        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // DELETE: api/Skills/remove-from-user/{userId}/{skillId}
    [HttpDelete("remove-from-user/{userId}/{skillId}")]
    public async Task<IActionResult> RemoveSkillFromUser(int userId, int skillId)
    {
        var userSkill = await _context.UserSkills
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);
        if (userSkill == null) return NotFound();
        _context.UserSkills.Remove(userSkill);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

