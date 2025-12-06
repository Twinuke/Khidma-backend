using khidma_backend.Data;
using khidma_backend.Models;
using khidma_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;

    public AuthController(AppDbContext context, JwtService jwtService, EmailService emailService)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public UserType UserType { get; set; }

        // ✅ FIX: Added ProfileBio to DTO
        public string? ProfileBio { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public object User { get; set; } = null!;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        // Check if email already exists
        var existingUserByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Check if phone number already exists
        var existingUserByPhone = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (existingUserByPhone != null)
        {
            return BadRequest(new { message = "Phone number is already registered" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create new user
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            PhoneNumber = request.PhoneNumber,
            UserType = request.UserType,
            CreatedAt = DateTime.UtcNow,
            // ✅ FIX: Assign Bio from request
            ProfileBio = request.ProfileBio 
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtService.GenerateToken(
            user.UserId,
            user.Email,
            user.UserType.ToString()
        );

        var response = new AuthResponse
        {
            Token = token,
            User = new 
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.UserType,
                user.ProfileBio,
                user.ProfileImageUrl
            }
        };

        return CreatedAtAction(nameof(Register), response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return BadRequest(new { message = "Invalid email or password" });
        }

        var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());

        return Ok(new 
        {
            token,
            user = new 
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.UserType,
                user.ProfileBio,
                user.ProfileImageUrl
            }
        });
    }
}