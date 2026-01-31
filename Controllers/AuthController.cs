using khidma_backend.Data;
using khidma_backend.Models;
using khidma_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace khidma_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, JwtService jwtService, EmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
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

    public class PasswordResetRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class PasswordResetUpdateDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for authenticated change-password (no reset token).
    /// </summary>
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    private static string CreateToken()
    {
        // 32 bytes => 256-bit token
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant(); // 64 hex chars
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string BuildPasswordResetDeepLink(string rawToken)
    {
        // Example: khidma://reset-password?token=...
        var baseLink = _configuration["AppLinks:PasswordResetDeepLinkBase"] ?? "khidma://reset-password";
        var separator = baseLink.Contains('?') ? "&" : "?";
        return $"{baseLink}{separator}token={Uri.EscapeDataString(rawToken)}";
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

    // ================== Password Reset Flow ==================

    /// <summary>
    /// Step 1: User submits email. We send a deep-link email if the user exists.
    /// Always returns 200 to avoid user enumeration.
    /// </summary>
    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
        }

        var rawToken = CreateToken();
        var tokenHash = HashToken(rawToken);

        var expiresMinutes = int.TryParse(_configuration["AppLinks:PasswordResetTokenExpiryMinutes"], out var m) ? m : 30;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var entity = new PasswordResetToken
        {
            UserId = user.UserId,
            Email = user.Email,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(entity);
        await _context.SaveChangesAsync();

        var deepLink = BuildPasswordResetDeepLink(rawToken);
        await _emailService.SendPasswordResetEmailAsync(user.Email, deepLink);

        return Ok(new
        {
            message = "If an account exists for this email, a reset link has been sent.",
            token = rawToken
        });
    }

    /// <summary>
    /// Step 2: Verify token when the user clicks the email link (the app calls this after deep-link open).
    /// Marks the token as verified (but not used).
    /// </summary>
    [HttpGet("password-reset/verify")]
    public async Task<IActionResult> VerifyPasswordResetToken([FromQuery][Required] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token is required." });

        var tokenHash = HashToken(token);
        var record = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (record == null)
            return BadRequest(new { message = "Invalid or expired token." });

        if (record.UsedAt != null)
            return BadRequest(new { message = "Token already used." });

        if (record.ExpiresAt <= DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired token." });

        if (record.VerifiedAt == null)
        {
            record.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Verified." });
    }

    /// <summary>
    /// Step 3: Update password. Requires a verified, non-expired, non-used token.
    /// </summary>
    [HttpPost("password-reset/update")]
    public async Task<IActionResult> UpdatePassword([FromBody] PasswordResetUpdateDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tokenHash = HashToken(request.Token);
        var record = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (record == null)
            return BadRequest(new { message = "Invalid or expired token." });

        if (record.UsedAt != null)
            return BadRequest(new { message = "Token already used." });

        if (record.ExpiresAt <= DateTime.UtcNow)
            return BadRequest(new { message = "Invalid or expired token." });

        if (record.VerifiedAt == null)
            return BadRequest(new { message = "Token not verified." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == record.UserId);
        if (user == null)
            return BadRequest(new { message = "Invalid token." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        record.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Password updated." });
    }

    /// <summary>
    /// Change password when logged in: verify current password, then update to new.
    /// If NewPassword equals CurrentPassword, only verification is performed (no update).
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _jwtService.GetUserIdFromClaims(User);
        if (userId == null)
            return Unauthorized(new { message = "Invalid or missing token." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
        if (user == null)
            return Unauthorized(new { message = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.CurrentPassword == request.NewPassword)
            return Ok(new { message = "Password verified.", verified = true });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Password updated.", updated = true });
    }

    /// <summary>
    /// Direct password reset: email + new password. No token. Demo only.
    /// </summary>
    [HttpPost("password-reset/direct")]
    public async Task<IActionResult> DirectPasswordReset([FromBody] DirectResetDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Email and new password required." });
        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (user == null)
            return BadRequest(new { message = "Account not found." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Password updated." });
    }

    public class DirectResetDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}