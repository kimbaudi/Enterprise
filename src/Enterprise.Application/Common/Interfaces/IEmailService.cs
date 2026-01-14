namespace Enterprise.Application.Common.Interfaces;

public interface IEmailService
{
    // Authentication emails
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string toEmail, string toName, string verificationToken, CancellationToken cancellationToken = default);

    // Security notification emails
    Task SendPasswordChangedEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    Task SendAccountLockedEmailAsync(string toEmail, string toName, DateTime lockoutEnd, CancellationToken cancellationToken = default);
    Task SendLoginFromNewDeviceEmailAsync(string toEmail, string toName, string ipAddress, string device, CancellationToken cancellationToken = default);

    // Two-Factor Authentication emails
    Task SendTwoFactorEnabledEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    Task SendTwoFactorDisabledEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    Task SendTwoFactorCodeEmailAsync(string toEmail, string toName, string code, CancellationToken cancellationToken = default);

    // Generic email sending
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);

    // Bulk operations
    Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
