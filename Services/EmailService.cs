using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace khidma_backend.Services;

/// <summary>
/// Simple SMTP email service for sending OTP codes.
/// Uses Gmail or any other SMTP provider based on configuration.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpHost = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
        _smtpUsername = _configuration["Smtp:Username"] ?? string.Empty;
        _smtpPassword = _configuration["Smtp:Password"] ?? string.Empty;
        _fromEmail = _configuration["Smtp:FromEmail"] ?? _smtpUsername;
        _fromName = _configuration["Smtp:FromName"] ?? "Khidma";
        _enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
    }

    /// <summary>
    /// Send email with OTP code. Falls back to console logging in development
    /// when SMTP credentials are not configured.
    /// </summary>
    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
    {
        try
        {
            var subject = "Your Khidma Verification Code";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #007AFF;'>Khidma Verification Code</h2>
                    <p>Hello,</p>
                    <p>Your verification code is:</p>
                    <div style='background-color: #f5f5f5; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; margin: 20px 0; border-radius: 8px;'>
                        {otpCode}
                    </div>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you didn't request this code, please ignore this email.</p>
                    <p style='color: #666; margin-top: 30px;'>Best regards,<br/>Khidma Team</p>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending OTP email: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generic email sender.
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            // If SMTP credentials are not configured, log to console (dev fallback)
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine("=== EMAIL (Development Mode) ===");
                Console.WriteLine($"To: {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {body}");
                Console.WriteLine("================================");
                return true;
            }

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _enableSsl,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}


