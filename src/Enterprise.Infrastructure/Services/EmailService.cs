using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset Request";
        var htmlBody = $@"
            <h2>Password Reset Request</h2>
            <p>Hello {toName},</p>
            <p>You have requested to reset your password. Please use the following token to reset your password:</p>
            <p><strong>{resetToken}</strong></p>
            <p>This token will expire in 1 hour.</p>
            <p>If you did not request this password reset, please ignore this email.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Enterprise!";
        var htmlBody = $@"
            <h2>Welcome to Enterprise!</h2>
            <p>Hello {toName},</p>
            <p>Your account has been successfully created.</p>
            <p>You can now log in and start using our services.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual email sending using SendGrid, AWS SES, or SMTP
        // For now, just log the email
        _logger.LogInformation(
            "Email would be sent to {ToEmail} with subject: {Subject}",
            toEmail,
            subject);

        _logger.LogDebug("Email body: {HtmlBody}", htmlBody);

        await Task.CompletedTask;
    }
}
