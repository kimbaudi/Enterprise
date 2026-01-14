# Email Service Setup Guide

## Overview

The Enterprise API uses SendGrid for email delivery. The email service supports:

- Password reset emails
- Welcome emails for new users
- Custom email sending for any purpose

## Implementation Details

**Service**: [EmailService.cs](../src/Enterprise.Infrastructure/Services/EmailService.cs)
**Interface**: [IEmailService.cs](../src/Enterprise.Application/Common/Interfaces/IEmailService.cs)
**Package**: SendGrid v9.29.3

## Configuration

### Development Setup (User Secrets)

1. **Get a SendGrid API Key**:
   - Sign up at [SendGrid.com](https://sendgrid.com)
   - Create an API key with "Mail Send" permissions
   - Copy the API key (you'll only see it once)

2. **Configure User Secrets**:

   ```bash
   cd src/Enterprise.WebApi
   dotnet user-secrets set "EmailSettings:SendGridApiKey" "YOUR_ACTUAL_SENDGRID_API_KEY"
   ```

3. **Optional: Configure From Email** (defaults are set in appsettings.json):

   ```bash
   dotnet user-secrets set "EmailSettings:FromEmail" "noreply@yourdomain.com"
   dotnet user-secrets set "EmailSettings:FromName" "Your Company Name"
   ```

### Production Setup (Environment Variables)

Set the following environment variables in your production environment:

```bash
EmailSettings__SendGridApiKey=your_production_api_key
EmailSettings__FromEmail=noreply@yourdomain.com
EmailSettings__FromName=Enterprise
```

**Docker**:

```yaml
environment:
  - EmailSettings__SendGridApiKey=your_api_key
  - EmailSettings__FromEmail=noreply@yourdomain.com
```

**Azure App Service**:

- Navigate to Configuration → Application Settings
- Add new setting: `EmailSettings__SendGridApiKey`

## Email Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `SendGridApiKey` | SendGrid API key for authentication | (empty - required) |
| `FromEmail` | Email address that appears as sender | `noreply@enterprise.com` |
| `FromName` | Display name for sender | `Enterprise` |

## Fallback Behavior

If the SendGrid API key is not configured:

- Email service will **log** emails instead of sending them
- No errors will be thrown (graceful degradation)
- Useful for development/testing without email setup

## Usage Examples

### In Command Handlers

```csharp
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;

    public ForgotPasswordCommandHandler(
        IEmailService emailService,
        IUserRepository userRepository)
    {
        _emailService = emailService;
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        var resetToken = GenerateResetToken();
        
        // Send password reset email
        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            user.FirstName,
            resetToken,
            cancellationToken);
            
        return Unit.Value;
    }
}
```

### Custom Email

```csharp
await _emailService.SendEmailAsync(
    toEmail: "user@example.com",
    subject: "Your Custom Subject",
    htmlBody: "<h1>Hello</h1><p>Your custom HTML content</p>",
    cancellationToken);
```

## Verifying Configuration

Check if email service is properly configured:

```bash
dotnet user-secrets list
```

You should see:

```
EmailSettings:SendGridApiKey = YOUR_SENDGRID_API_KEY_HERE
```

## Troubleshooting

### Email Not Sending

1. **Check API Key**: Verify it's set in user secrets or environment variables
2. **Check Logs**: Look for warnings in application logs:

   ```
   SendGrid API key not configured. Email would be sent to...
   ```

3. **Verify SendGrid Account**: Ensure your SendGrid account is active and not suspended

### SendGrid Errors

Common error codes:

- `401 Unauthorized`: Invalid API key
- `403 Forbidden`: API key lacks permissions
- `429 Too Many Requests`: Rate limit exceeded

Check logs for detailed error messages:

```csharp
_logger.LogError("Failed to send email to {ToEmail}. Status: {StatusCode}, Error: {Error}", ...)
```

## Testing Email Service

### Unit Tests

Mock the `IEmailService` interface in your tests:

```csharp
var emailServiceMock = new Mock<IEmailService>();
emailServiceMock
    .Setup(x => x.SendWelcomeEmailAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
```

### Integration Testing

For development, leave the API key empty to see email content in logs without actually sending.

## Security Best Practices

✅ **DO**:

- Store API keys in User Secrets (dev) or Environment Variables (prod)
- Use different SendGrid API keys for development/staging/production
- Rotate API keys regularly
- Use API keys with minimum required permissions

❌ **DON'T**:

- Commit API keys to source control
- Share API keys in team chats or documentation
- Use production keys in development environments
- Grant more permissions than needed (only "Mail Send")

## Cost Considerations

SendGrid offers:

- **Free Tier**: 100 emails/day forever
- **Essentials**: $19.95/month for 50,000 emails
- **Pro**: Custom pricing for higher volumes

Plan accordingly based on your user base and email frequency.

## Related Files

- [EmailService.cs](../src/Enterprise.Infrastructure/Services/EmailService.cs) - Implementation
- [IEmailService.cs](../src/Enterprise.Application/Common/Interfaces/IEmailService.cs) - Interface
- [appsettings.json](../src/Enterprise.WebApi/appsettings.json) - Configuration structure
- [SECURITY-CONFIGURATION.md](./SECURITY-CONFIGURATION.md) - General secrets management
