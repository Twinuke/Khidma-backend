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

    // DTOs for request/response
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

        // ✅ FIX: Added ProfileBio so it can be saved during registration
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
        public UserInfo User { get; set; } = null!;
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public string? ProfileBio { get; set; }
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
            ProfileBio = request.ProfileBio // ✅ FIX: Assigning Bio
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
            User = new UserInfo
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                ProfileBio = user.ProfileBio
            }
        };

        return CreatedAtAction(nameof(Register), response);
    }

    // ... (Keep the rest of your OTP endpoints: SendOtp, VerifyOtp, VerifyPhone, etc. exactly as they were) ...
    // To save space, I am not repeating the OTP logic here, but DO NOT DELETE IT from your file.
    // Just ensure the RegisterRequest class and Register method are updated as above.
    
    // --- COPY PASTE THE REST OF YOUR OTP METHODS HERE IF YOU OVERWRITE THE FILE ---
    // (SendOtp, VerifyOtp, VerifyPhone, ConfirmOtp, RequestEmailOtp, VerifyEmailOtp, ResendEmailOtp)
}