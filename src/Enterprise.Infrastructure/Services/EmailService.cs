using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Enterprise.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _sendGridApiKey = _configuration["EmailSettings:SendGridApiKey"];
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@enterprise.com";
        _fromName = _configuration["EmailSettings:FromName"] ?? "Enterprise";
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
        // If SendGrid API key is not configured, fall back to logging only (for development)
        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
            _logger.LogWarning(
                "SendGrid API key not configured. Email would be sent to {ToEmail} with subject: {Subject}",
                toEmail,
                subject);
            _logger.LogDebug("Email body: {HtmlBody}", htmlBody);
            return;
        }

        try
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlBody, htmlBody);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email successfully sent to {ToEmail} with subject: {Subject}",
                    toEmail,
                    subject);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to send email to {ToEmail}. Status: {StatusCode}, Error: {Error}",
                    toEmail,
                    response.StatusCode,
                    errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending email to {ToEmail} with subject: {Subject}",
                toEmail,
                subject);
            throw;
        }
    }
}
