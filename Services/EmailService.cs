using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace khidma_backend.Services;

/// <summary>
/// Service for sending emails via SMTP
/// Supports Gmail SMTP and other SMTP providers
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
        _smtpUsername = _configuration["Smtp:Username"] ?? "";
        _smtpPassword = _configuration["Smtp:Password"] ?? "";
        _fromEmail = _configuration["Smtp:FromEmail"] ?? _smtpUsername;
        _fromName = _configuration["Smtp:FromName"] ?? "Khidma";
        _enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
    }

    /// <summary>
    /// Send email with OTP code
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
    /// Send generic email
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            // In development, if SMTP is not configured, log to console
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                Console.WriteLine($"=== EMAIL (Development Mode) ===");
                Console.WriteLine($"To: {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {body}");
                Console.WriteLine($"===============================");
                return true; // Return true in development to allow testing
            }

            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.EnableSsl = _enableSsl;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_fromEmail, _fromName);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;

                    await client.SendMailAsync(message);
                }
            }

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


