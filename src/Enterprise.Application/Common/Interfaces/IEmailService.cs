namespace Enterprise.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
