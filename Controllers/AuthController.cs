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

    // ========== DTOs ==========

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
    }

    // Step 1 of registration – send OTP only
    public class RegisterStartRequest : RegisterRequest
    {
    }

    // Step 2 of registration – confirm OTP and create account
    public class RegisterCompleteRequest : RegisterRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class VerifyLoginOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResendLoginOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
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

    // ========== Helper methods ==========

    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private async Task CreateEmailOtpAsync(string email, string otpCode)
    {
        // Invalidate existing, not-yet-used OTPs for this email
        var existing = await _context.EmailVerifications
            .Where(ev => ev.Email == email && !ev.IsVerified && ev.ExpireAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var ev in existing)
        {
            ev.IsVerified = true;
        }

        var emailVerification = new EmailVerification
        {
            Email = email,
            Code = otpCode,
            ExpireAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send email (or log in development)
        await _emailService.SendOtpEmailAsync(email, otpCode);
    }

    private async Task<EmailVerification?> ValidateEmailOtpAsync(string email, string otpCode)
    {
        var emailVerification = await _context.EmailVerifications
            .Where(ev => ev.Email == email
                      && ev.Code == otpCode
                      && !ev.IsVerified
                      && ev.ExpireAt > DateTime.UtcNow)
            .OrderByDescending(ev => ev.CreatedAt)
            .FirstOrDefaultAsync();

        if (emailVerification == null)
        {
            return null;
        }

        emailVerification.IsVerified = true;
        await _context.SaveChangesAsync();

        return emailVerification;
    }

    private AuthResponse BuildAuthResponse(User user, string token)
    {
        return new AuthResponse
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
    }

    // ========== Registration with Email OTP ==========

    /// <summary>
    /// Step 1: Start registration. If email not used, generate OTP and send to email.
    /// </summary>
    [HttpPost("register-start")]
    public async Task<IActionResult> RegisterStart([FromBody] RegisterStartRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if email already exists
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already in use" });
        }

        var otpCode = GenerateOtp();
        await CreateEmailOtpAsync(request.Email, otpCode);

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        return Ok(new
        {
            message = "Verification code sent to your email. Please enter the OTP to complete registration.",
            otp = isDevelopment ? otpCode : null
        });
    }

    /// <summary>
    /// Step 2: Complete registration after email OTP is verified.
    /// </summary>
    [HttpPost("register-complete")]
    public async Task<IActionResult> RegisterComplete([FromBody] RegisterCompleteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Make sure email is still not registered
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already in use" });
        }

        // Validate OTP
        var verification = await ValidateEmailOtpAsync(request.Email, request.Otp);
        if (verification == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP code" });
        }

        // Create new user
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            PhoneNumber = request.PhoneNumber,
            UserType = request.UserType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());
        var response = BuildAuthResponse(user, token);

        return CreatedAtAction(nameof(RegisterComplete), response);
    }

    // ========== Login + Email OTP (2FA) ==========

    /// <summary>
    /// Step 1: Password check. If valid, send OTP to email. No JWT returned here.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Generate and send OTP
        var otpCode = GenerateOtp();
        await CreateEmailOtpAsync(user.Email, otpCode);

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        return Ok(new
        {
            message = "Login successful. A verification code has been sent to your email. Please enter the OTP to complete login.",
            email = user.Email,
            requiresOtp = true,
            otp = isDevelopment ? otpCode : null
        });
    }

    /// <summary>
    /// Step 2: Verify login OTP and return JWT token.
    /// </summary>
    [HttpPost("verify-login-otp")]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var verification = await ValidateEmailOtpAsync(request.Email, request.Otp);
        if (verification == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP code" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());
        var response = BuildAuthResponse(user, token);

        return Ok(response);
    }

    /// <summary>
    /// Resend login OTP without re-entering password.
    /// </summary>
    [HttpPost("resend-login-otp")]
    public async Task<IActionResult> ResendLoginOtp([FromBody] ResendLoginOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var otpCode = GenerateOtp();
        await CreateEmailOtpAsync(request.Email, otpCode);

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        return Ok(new
        {
            message = "A new verification code has been sent to your email.",
            otp = isDevelopment ? otpCode : null
        });
    }

    // ========== Current user ==========

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = _jwtService.GetUserIdFromClaims(User);
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var userInfo = new UserInfo
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            UserType = user.UserType,
            ProfileBio = user.ProfileBio
        };

        return Ok(userInfo);
    }
}


