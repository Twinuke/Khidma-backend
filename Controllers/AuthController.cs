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

    /// <summary>
    /// Step 3: Register user (only if phone is verified)
    /// Allows registration only if phoneNumber has been verified via OTP
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if phone number is provided and verified
        if (string.IsNullOrEmpty(request.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        // Verify that the phone number has been verified via OTP
        var verifiedPhone = await _context.PhoneVerifications
            .Where(pv => pv.PhoneNumber == request.PhoneNumber 
                     && pv.IsVerified 
                     && pv.ExpireAt > DateTime.UtcNow)
            .OrderByDescending(pv => pv.CreatedAt)
            .FirstOrDefaultAsync();

        if (verifiedPhone == null)
        {
            return BadRequest(new { message = "Phone number must be verified before registration. Please complete phone verification first." });
        }

        // Check if email already exists
        var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        // Check if phone number already exists in Users table
        var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (existingUserByPhone != null)
        {
            return BadRequest(new { message = "Phone number is already registered" });
        }

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create new user
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

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());

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

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());

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

        return Ok(response);
    }

    /// <summary>
    /// Get current user info from JWT token
    /// </summary>
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

    /// <summary>
    /// Send OTP to phone number
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Generate 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Invalidate any existing OTPs for this phone number
        var existingOtps = await _context.OtpCodes
            .Where(o => o.PhoneNumber == request.PhoneNumber && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var existingOtp in existingOtps)
        {
            existingOtp.IsUsed = true;
        }

        // Create new OTP (expires in 5 minutes)
        var newOtp = new OtpCode
        {
            PhoneNumber = request.PhoneNumber,
            Code = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        _context.OtpCodes.Add(newOtp);
        await _context.SaveChangesAsync();

        // TODO: In production, send SMS via Twilio, AWS SNS, or similar service
        // For now, we'll just return the OTP in development (remove this in production!)
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        if (isDevelopment)
        {
            // Log OTP to console for development
            Console.WriteLine($"OTP for {request.PhoneNumber}: {otpCode}");
        }

        return Ok(new { 
            message = "OTP sent successfully",
            // Remove this in production!
            otp = isDevelopment ? otpCode : null
        });
    }

    /// <summary>
    /// Verify OTP and authenticate user
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find valid OTP
        var otp = await _context.OtpCodes
            .Where(o => o.PhoneNumber == request.PhoneNumber 
                     && o.Code == request.Otp 
                     && !o.IsUsed 
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP" });
        }

        // Mark OTP as used
        otp.IsUsed = true;
        await _context.SaveChangesAsync();

        // Check if user exists with this phone number
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user != null)
        {
            // User exists, generate token and return
            var token = _jwtService.GenerateToken(user.UserId, user.Email, user.UserType.ToString());
            
            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = token,
                user = new UserInfo
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserType = user.UserType,
                    ProfileBio = user.ProfileBio
                },
                isNewUser = false
            });
        }
        else
        {
            // New user, return without token (they need to complete registration)
            return Ok(new
            {
                message = "OTP verified. Please complete registration.",
                isNewUser = true
            });
        }
    }

    public class SendOtpRequest
    {
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    // ========== Two-Step Phone Verification Endpoints ==========

    /// <summary>
    /// Step 1: Verify phone number and send OTP
    /// Checks if phone number is valid and not already registered, then sends OTP
    /// </summary>
    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if phone number already exists in Users table
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Phone number is already registered" });
        }

        // Generate 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Invalidate any existing unverified OTPs for this phone number
        var existingVerifications = await _context.PhoneVerifications
            .Where(pv => pv.PhoneNumber == request.PhoneNumber && !pv.IsVerified && pv.ExpireAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var existingVerification in existingVerifications)
        {
            existingVerification.IsVerified = true; // Mark as used/invalid
        }

        // Create new phone verification (expires in 5 minutes)
        var phoneVerification = new PhoneVerification
        {
            PhoneNumber = request.PhoneNumber,
            Code = otpCode,
            ExpireAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PhoneVerifications.Add(phoneVerification);
        await _context.SaveChangesAsync();

        // TODO: In production, send SMS via Twilio, AWS SNS, or similar service
        // For now, we'll just return the OTP in development (remove this in production!)
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        if (isDevelopment)
        {
            // Log OTP to console for development
            Console.WriteLine($"OTP for {request.PhoneNumber}: {otpCode}");
        }

        return Ok(new { 
            message = "OTP sent successfully to your phone number",
            // Remove this in production!
            otp = isDevelopment ? otpCode : null
        });
    }

    /// <summary>
    /// Step 2: Confirm OTP code
    /// Validates the OTP and marks the phone number as verified
    /// </summary>
    [HttpPost("confirm-otp")]
    public async Task<IActionResult> ConfirmOtp([FromBody] ConfirmOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find valid, unverified OTP
        var phoneVerification = await _context.PhoneVerifications
            .Where(pv => pv.PhoneNumber == request.PhoneNumber 
                     && pv.Code == request.Otp 
                     && !pv.IsVerified 
                     && pv.ExpireAt > DateTime.UtcNow)
            .OrderByDescending(pv => pv.CreatedAt)
            .FirstOrDefaultAsync();

        if (phoneVerification == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP code" });
        }

        // Mark as verified
        phoneVerification.IsVerified = true;
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Phone number verified successfully",
            phoneNumber = request.PhoneNumber
        });
    }

    // DTOs for phone verification
    public class VerifyPhoneRequest
    {
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ConfirmOtpRequest
    {
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    // ========== Email OTP Endpoints ==========

    /// <summary>
    /// Request email OTP - sends a 6-digit code to the user's email
    /// </summary>
    [HttpPost("request-email-otp")]
    public async Task<IActionResult> RequestEmailOtp([FromBody] RequestEmailOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate email format
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest(new { message = "Invalid email format" });
        }

        // Check if email already exists in Users table (for registration)
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null && request.Purpose == "register")
        {
            return BadRequest(new { message = "Email is already registered" });
        }

        // Generate 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Invalidate any existing unverified OTPs for this email
        var existingVerifications = await _context.EmailVerifications
            .Where(ev => ev.Email == request.Email && !ev.IsVerified && ev.ExpireAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var existingVerification in existingVerifications)
        {
            existingVerification.IsVerified = true; // Mark as used/invalid
        }

        // Create new email verification (expires in 5 minutes)
        var emailVerification = new EmailVerification
        {
            Email = request.Email,
            Code = otpCode,
            ExpireAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send email with OTP
        var emailSent = await _emailService.SendOtpEmailAsync(request.Email, otpCode);

        if (!emailSent)
        {
            // If email fails, still return success in development but log the OTP
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                Console.WriteLine($"OTP for {request.Email}: {otpCode}");
                return Ok(new { 
                    message = "OTP generated. Check console for code (development mode)",
                    // Remove this in production!
                    otp = isDevelopment ? otpCode : null
                });
            }
            return StatusCode(500, new { message = "Failed to send email. Please try again later." });
        }

        var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        return Ok(new { 
            message = "OTP sent successfully to your email",
            // Remove this in production!
            otp = isDev ? otpCode : null
        });
    }

    /// <summary>
    /// Verify email OTP code
    /// </summary>
    [HttpPost("verify-email-otp")]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find valid, unverified OTP
        var emailVerification = await _context.EmailVerifications
            .Where(ev => ev.Email == request.Email 
                     && ev.Code == request.Otp 
                     && !ev.IsVerified 
                     && ev.ExpireAt > DateTime.UtcNow)
            .OrderByDescending(ev => ev.CreatedAt)
            .FirstOrDefaultAsync();

        if (emailVerification == null)
        {
            return BadRequest(new { message = "Invalid or expired OTP code" });
        }

        // Mark as verified
        emailVerification.IsVerified = true;
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "Email verified successfully",
            email = request.Email
        });
    }

    /// <summary>
    /// Resend email OTP - generates and sends a new OTP code
    /// </summary>
    [HttpPost("resend-email-otp")]
    public async Task<IActionResult> ResendEmailOtp([FromBody] ResendEmailOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate email format
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest(new { message = "Invalid email format" });
        }

        // Generate new 6-digit OTP
        var random = new Random();
        var otpCode = random.Next(100000, 999999).ToString();

        // Invalidate any existing unverified OTPs for this email
        var existingVerifications = await _context.EmailVerifications
            .Where(ev => ev.Email == request.Email && !ev.IsVerified && ev.ExpireAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var existingVerification in existingVerifications)
        {
            existingVerification.IsVerified = true; // Mark as used/invalid
        }

        // Create new email verification (expires in 5 minutes)
        var emailVerification = new EmailVerification
        {
            Email = request.Email,
            Code = otpCode,
            ExpireAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send email with OTP
        var emailSent = await _emailService.SendOtpEmailAsync(request.Email, otpCode);

        if (!emailSent)
        {
            // If email fails, still return success in development but log the OTP
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                Console.WriteLine($"Resent OTP for {request.Email}: {otpCode}");
                return Ok(new { 
                    message = "OTP regenerated. Check console for code (development mode)",
                    // Remove this in production!
                    otp = isDevelopment ? otpCode : null
                });
            }
            return StatusCode(500, new { message = "Failed to send email. Please try again later." });
        }

        var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        return Ok(new { 
            message = "New OTP sent successfully to your email",
            // Remove this in production!
            otp = isDev ? otpCode : null
        });
    }

    // DTOs for email OTP
    public class RequestEmailOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string Purpose { get; set; } = "register"; // "register" or "login"
    }

    public class VerifyEmailOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResendEmailOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

