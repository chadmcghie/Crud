using System.Text.RegularExpressions;
using App.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MockEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockEmailService> _logger;
    private readonly List<EmailMessage> _sentEmails = new();
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public MockEmailService(IConfiguration configuration, ILogger<MockEmailService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be null or empty", nameof(email));

        if (string.IsNullOrWhiteSpace(resetToken))
            throw new ArgumentException("Reset token cannot be null or empty", nameof(resetToken));

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Please provide a valid email address", nameof(email));

        cancellationToken.ThrowIfCancellationRequested();

        // Get configuration
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@example.com";
        var fromName = _configuration["Email:FromName"] ?? "CRUD App";
        var resetPasswordUrl = _configuration["Email:ResetPasswordUrl"] ?? "http://localhost:4200/reset-password";

        // Build the reset URL with token
        var fullResetUrl = $"{resetPasswordUrl}?token={resetToken}";

        // Create email message
        var emailMessage = new EmailMessage
        {
            To = email,
            From = $"{fromName} <{fromAddress}>",
            Subject = "Password Reset Request",
            Body = GenerateEmailBody(email, fullResetUrl),
            SentAt = DateTime.UtcNow
        };

        // Simulate email sending delay
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        // Store email in memory for testing/verification
        _sentEmails.Add(emailMessage);

        // Log the email details (in production, this would actually send the email)
        _logger.LogInformation(
            "Password reset email sent to {Email} with token {Token} (Mock Service - Not Actually Sent)",
            email,
            resetToken.Substring(0, Math.Min(10, resetToken.Length)) + "...");

        _logger.LogDebug(
            "Email Details - From: {From}, To: {To}, Subject: {Subject}",
            emailMessage.From,
            emailMessage.To,
            emailMessage.Subject);
    }

    /// <summary>
    /// Gets all emails that have been "sent" by this mock service
    /// </summary>
    public IReadOnlyList<EmailMessage> GetSentEmails() => _sentEmails.AsReadOnly();

    /// <summary>
    /// Clears all stored emails from memory
    /// </summary>
    public void ClearSentEmails() => _sentEmails.Clear();

    private static string GenerateEmailBody(string email, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Password Reset Request</h2>
        
        <p>Hello,</p>
        
        <p>We received a request to reset the password for your account associated with {email}.</p>
        
        <p>To reset your password, please click the link below:</p>
        
        <p style='margin: 25px 0;'>
            <a href='{resetUrl}' 
               style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>
                Reset Password
            </a>
        </p>
        
        <p>Or copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #007bff;'>{resetUrl}</p>
        
        <p><strong>This link will expire in 1 hour for security reasons.</strong></p>
        
        <p>If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
        
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        
        <p style='font-size: 12px; color: #666;'>
            This is an automated message. Please do not reply to this email.
        </p>
    </div>
</body>
</html>";
    }

    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
