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
    private readonly IResiliencePolicyProvider? _policyProvider;
    private readonly string? _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration,
        IResiliencePolicyProvider? policyProvider = null)
    {
        _logger = logger;
        _configuration = configuration;
        _policyProvider = policyProvider;
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

    public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationToken, CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email Address";
        var htmlBody = $@"
            <h2>Email Verification</h2>
            <p>Hello {toName},</p>
            <p>Please verify your email address by using the following token:</p>
            <p><strong>{verificationToken}</strong></p>
            <p>This token will expire in 24 hours.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
    {
        var subject = "Password Changed Successfully";
        var htmlBody = $@"
            <h2>Password Changed</h2>
            <p>Hello {toName},</p>
            <p>Your password has been successfully changed.</p>
            <p>If you did not make this change, please contact support immediately.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendAccountLockedEmailAsync(string toEmail, string toName, DateTime lockoutEnd, CancellationToken cancellationToken = default)
    {
        var remainingMinutes = (int)(lockoutEnd - DateTime.UtcNow).TotalMinutes;
        var subject = "Account Temporarily Locked";
        var htmlBody = $@"
            <h2>Account Locked</h2>
            <p>Hello {toName},</p>
            <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
            <p>Your account will be automatically unlocked in approximately {remainingMinutes} minutes.</p>
            <p>If you did not attempt to log in, please contact support immediately.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendLoginFromNewDeviceEmailAsync(string toEmail, string toName, string ipAddress, string device, CancellationToken cancellationToken = default)
    {
        var subject = "New Login Detected";
        var htmlBody = $@"
            <h2>New Login Detected</h2>
            <p>Hello {toName},</p>
            <p>We detected a new login to your account:</p>
            <ul>
                <li><strong>Device:</strong> {device}</li>
                <li><strong>IP Address:</strong> {ipAddress}</li>
                <li><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
            </ul>
            <p>If this was not you, please change your password immediately and contact support.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendTwoFactorEnabledEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
    {
        var subject = "Two-Factor Authentication Enabled";
        var htmlBody = $@"
            <h2>Two-Factor Authentication Enabled</h2>
            <p>Hello {toName},</p>
            <p>Two-factor authentication has been successfully enabled on your account.</p>
            <p>You will now need to enter a verification code from your authenticator app when logging in.</p>
            <p>Make sure to save your recovery codes in a secure location.</p>
            <p>If you did not enable this feature, please contact support immediately.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendTwoFactorDisabledEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
    {
        var subject = "Two-Factor Authentication Disabled";
        var htmlBody = $@"
            <h2>Two-Factor Authentication Disabled</h2>
            <p>Hello {toName},</p>
            <p>Two-factor authentication has been disabled on your account.</p>
            <p>Your account is now less secure. We recommend re-enabling two-factor authentication to protect your account.</p>
            <p>If you did not disable this feature, please contact support immediately.</p>
            <p>Best regards,<br/>Enterprise Team</p>
        ";

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendTwoFactorCodeEmailAsync(string toEmail, string toName, string code, CancellationToken cancellationToken = default)
    {
        var subject = "Your Two-Factor Authentication Code";
        var htmlBody = $@"
            <h2>Two-Factor Authentication Code</h2>
            <p>Hello {toName},</p>
            <p>Your verification code is:</p>
            <p style='font-size: 24px; font-weight: bold; letter-spacing: 5px;'>{code}</p>
            <p>This code will expire in 10 minutes.</p>
            <p>If you did not request this code, please ignore this email and contact support.</p>
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

        // Execute email sending with resilience policy if available
        if (_policyProvider != null)
        {
            await _policyProvider.ExecuteExternalApiOperationAsync(async () =>
            {
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

                        // Throw exception to trigger retry/circuit breaker for server errors
                        if ((int)response.StatusCode >= 500)
                        {
                            throw new HttpRequestException($"SendGrid server error: {response.StatusCode}");
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Re-throw HTTP exceptions to be handled by Polly
                    throw;
                }
                catch (TaskCanceledException)
                {
                    // Re-throw timeout exceptions to be handled by Polly
                    throw;
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
            }, cancellationToken);
        }
        else
        {
            // No policy provider - send directly (for testing scenarios)
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

                    if ((int)response.StatusCode >= 500)
                    {
                        throw new HttpRequestException($"SendGrid server error: {response.StatusCode}");
                    }
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

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        // Use the overload that doesn't require toName
        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_sendGridApiKey))
        {
            _logger.LogWarning(
                "SendGrid API key not configured. Bulk email would be sent to {Count} recipients with subject: {Subject}",
                toEmails.Count(),
                subject);
            return;
        }

        try
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);

            var tasks = toEmails.Select(async email =>
            {
                try
                {
                    var to = new EmailAddress(email);
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlBody, htmlBody);
                    var response = await client.SendEmailAsync(msg, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Bulk email sent successfully to {Email}", email);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send bulk email to {Email}. Status: {StatusCode}", email, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending bulk email to {Email}", email);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Bulk email completed for {Count} recipients", toEmails.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending bulk emails");
            throw;
        }
    }
}
