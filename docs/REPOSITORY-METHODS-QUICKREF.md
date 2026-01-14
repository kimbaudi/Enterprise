# Repository & Service Methods - Quick Reference

## IAuditLogRepository - New Methods

### Recent Logs

```csharp
// Get last 50 audit logs
var recentLogs = await _auditLogRepository.GetRecentAsync(50, cancellationToken);
```

### IP Address Filtering

```csharp
// Get logs from specific IP
var ipLogs = await _auditLogRepository.GetByIpAddressAsync(
    "192.168.1.100", 
    pageNumber: 1, 
    pageSize: 20, 
    cancellationToken);
```

### Entity History

```csharp
// Get complete audit trail for an entity
var history = await _auditLogRepository.GetEntityHistoryAsync(
    "Product", 
    productId.ToString(), 
    cancellationToken);
```

### Advanced Search

```csharp
// Search with multiple filters
var (logs, totalCount) = await _auditLogRepository.SearchAsync(
    entityName: "User",
    entityId: userId.ToString(),
    userId: adminId,
    action: "Update",
    ipAddress: "192.168.1.100",
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow,
    pageNumber: 1,
    pageSize: 50,
    cancellationToken);

// Search with partial filters (nulls ignored)
var (allUserLogs, count) = await _auditLogRepository.SearchAsync(
    entityName: "User",
    entityId: null,           // All users
    userId: null,             // Any admin
    action: null,             // Any action
    ipAddress: null,          // Any IP
    startDate: null,          // No start date
    endDate: null,            // No end date
    pageNumber: 1,
    pageSize: 100,
    cancellationToken);
```

### Cleanup Operations

```csharp
// Delete logs older than 90 days (GDPR compliance)
var deletedCount = await _auditLogRepository.DeleteOlderThanAsync(
    DateTime.UtcNow.AddDays(-90), 
    cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Delete all logs for a deleted entity
var deletedLogs = await _auditLogRepository.DeleteByEntityAsync(
    "Product", 
    deletedProductId.ToString(), 
    cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

---

## IRefreshTokenRepository - New Methods

### Active Tokens

```csharp
// Get all active tokens for a user (sorted by newest)
var activeTokens = await _refreshTokenRepository.GetActiveTokensForUserAsync(
    userId, 
    cancellationToken);

// Count user's active sessions
var sessionCount = await _refreshTokenRepository.CountActiveTokensByUserAsync(
    userId, 
    cancellationToken);

// Count total active sessions across system
var totalSessions = await _refreshTokenRepository.CountTotalActiveTokensAsync(
    cancellationToken);
```

### Token Revocation

```csharp
// Revoke specific token (logout from one device)
await _refreshTokenRepository.RevokeTokenAsync(token, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Revoke all user tokens (logout from all devices)
await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

### Token Cleanup

```csharp
// Get expired tokens (for monitoring)
var expiredTokens = await _refreshTokenRepository.GetExpiredTokensAsync(cancellationToken);

// Delete expired tokens
await _refreshTokenRepository.DeleteExpiredTokensAsync(cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Delete revoked tokens (cleanup)
await _refreshTokenRepository.DeleteRevokedTokensAsync(cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

// Delete tokens older than 30 days
await _refreshTokenRepository.DeleteOlderThanAsync(
    DateTime.UtcNow.AddDays(-30), 
    cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

---

## IEmailService - New Methods

### Security Notifications

#### Password Changed

```csharp
await _emailService.SendPasswordChangedEmailAsync(
    user.Email, 
    user.FirstName, 
    cancellationToken);
```

#### Account Locked

```csharp
await _emailService.SendAccountLockedEmailAsync(
    user.Email, 
    user.FirstName, 
    user.LockoutEnd.Value, 
    cancellationToken);
```

#### New Device Login

```csharp
await _emailService.SendLoginFromNewDeviceEmailAsync(
    user.Email, 
    user.FirstName, 
    ipAddress: "192.168.1.100",
    device: "Chrome on Windows 11", 
    cancellationToken);
```

### Two-Factor Authentication Emails

#### 2FA Enabled

```csharp
await _emailService.SendTwoFactorEnabledEmailAsync(
    user.Email, 
    user.FirstName, 
    cancellationToken);
```

#### 2FA Disabled

```csharp
await _emailService.SendTwoFactorDisabledEmailAsync(
    user.Email, 
    user.FirstName, 
    cancellationToken);
```

#### Email 2FA Code (Backup Method)

```csharp
var code = GenerateRandomCode(); // 6-digit code
await _emailService.SendTwoFactorCodeEmailAsync(
    user.Email, 
    user.FirstName, 
    code, 
    cancellationToken);
```

### Email Verification

```csharp
await _emailService.SendEmailVerificationAsync(
    user.Email, 
    user.FirstName, 
    verificationToken, 
    cancellationToken);
```

### Enhanced Email Sending

#### With Recipient Name

```csharp
await _emailService.SendEmailAsync(
    toEmail: "user@example.com",
    toName: "John Doe",
    subject: "Custom Notification",
    htmlBody: "<h1>Hello John!</h1><p>Your custom message here.</p>",
    cancellationToken);
```

#### Bulk Emails

```csharp
var recipients = new[] { "user1@example.com", "user2@example.com", "user3@example.com" };
await _emailService.SendBulkEmailAsync(
    recipients,
    subject: "System Maintenance Notification",
    htmlBody: "<h2>Scheduled Maintenance</h2><p>System will be down for maintenance...</p>",
    cancellationToken);
```

---

## Common Usage Patterns

### Admin Dashboard Statistics

```csharp
public async Task<DashboardStatsResponse> GetDashboardStats(CancellationToken ct)
{
    var totalLogs = await _auditLogRepository.GetCountAsync(ct);
    var recentLogs = await _auditLogRepository.GetRecentAsync(10, ct);
    var activeSessions = await _refreshTokenRepository.CountTotalActiveTokensAsync(ct);
    
    return new DashboardStatsResponse
    {
        TotalAuditLogs = totalLogs,
        RecentLogs = recentLogs,
        ActiveSessions = activeSessions
    };
}
```

### Security Monitoring

```csharp
// Monitor failed login attempts from IP
var failedLogins = await _auditLogRepository.SearchAsync(
    entityName: "User",
    entityId: null,
    userId: null,
    action: "LoginFailed",
    ipAddress: suspiciousIp,
    startDate: DateTime.UtcNow.AddHours(-1),
    endDate: DateTime.UtcNow,
    pageNumber: 1,
    pageSize: 100,
    cancellationToken);

if (failedLogins.TotalCount > 10)
{
    // Send security alert
    await _emailService.SendEmailAsync(
        adminEmail,
        "Security Alert",
        $"Multiple failed logins from IP: {suspiciousIp}",
        cancellationToken);
}
```

### Session Management

```csharp
// Limit concurrent sessions per user
var activeSessionCount = await _refreshTokenRepository.CountActiveTokensByUserAsync(
    userId, 
    cancellationToken);

if (activeSessionCount >= 5) // Max 5 devices
{
    // Revoke oldest token or all tokens
    await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    await _emailService.SendEmailAsync(
        user.Email,
        user.FirstName,
        "Session Limit Reached",
        "You've been logged out from all devices due to session limit.",
        cancellationToken);
}
```

### Scheduled Cleanup Job

```csharp
public async Task CleanupOldDataAsync(CancellationToken ct)
{
    // Clean audit logs older than 90 days
    var deletedLogs = await _auditLogRepository.DeleteOlderThanAsync(
        DateTime.UtcNow.AddDays(-90), 
        ct);
    
    // Clean expired tokens
    await _refreshTokenRepository.DeleteExpiredTokensAsync(ct);
    
    // Clean revoked tokens older than 7 days
    await _refreshTokenRepository.DeleteOlderThanAsync(
        DateTime.UtcNow.AddDays(-7), 
        ct);
    
    await _unitOfWork.SaveChangesAsync(ct);
    
    _logger.LogInformation(
        "Cleanup completed: {LogsDeleted} audit logs deleted", 
        deletedLogs);
}
```

### Password Change Workflow

```csharp
public async Task ChangePasswordAsync(Guid userId, string newPassword, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(userId, ct);
    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    await _userRepository.UpdateAsync(user, ct);
    
    // Revoke all refresh tokens (force re-login)
    await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    
    // Send notification email
    await _emailService.SendPasswordChangedEmailAsync(
        user.Email, 
        user.FirstName, 
        ct);
}
```

### 2FA Setup Workflow

```csharp
public async Task Enable2FAAsync(Guid userId, string code, CancellationToken ct)
{
    // ... verify code and enable 2FA ...
    
    var user = await _userRepository.GetByIdAsync(userId, ct);
    user.TwoFactorEnabled = true;
    await _userRepository.UpdateAsync(user, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    
    // Send confirmation email
    await _emailService.SendTwoFactorEnabledEmailAsync(
        user.Email, 
        user.FirstName, 
        ct);
}
```

### Suspicious Activity Detection

```csharp
public async Task CheckSuspiciousLoginAsync(string userId, string ipAddress, CancellationToken ct)
{
    // Check recent logins from different IPs
    var recentLogins = await _auditLogRepository.SearchAsync(
        entityName: "User",
        entityId: userId,
        userId: null,
        action: "Login",
        ipAddress: null,
        startDate: DateTime.UtcNow.AddDays(-1),
        endDate: DateTime.UtcNow,
        pageNumber: 1,
        pageSize: 100,
        ct);
    
    var uniqueIPs = recentLogins.Logs
        .Select(l => l.IpAddress)
        .Distinct()
        .Count();
    
    if (uniqueIPs > 3) // Login from 3+ different IPs in 24 hours
    {
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId), ct);
        await _emailService.SendEmailAsync(
            user.Email,
            user.FirstName,
            "Suspicious Activity Detected",
            "We detected logins from multiple locations. Please secure your account.",
            ct);
    }
}
```

---

## Background Job Examples

### Token Cleanup Job (Hangfire)

```csharp
[AutomaticRetry(Attempts = 3)]
public async Task CleanupExpiredTokensJob()
{
    await _refreshTokenRepository.DeleteExpiredTokensAsync(default);
    await _unitOfWork.SaveChangesAsync(default);
}
```

### Audit Log Archival Job

```csharp
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public async Task ArchiveOldAuditLogsJob()
{
    var oldLogs = await _auditLogRepository.SearchAsync(
        entityName: null,
        entityId: null,
        userId: null,
        action: null,
        ipAddress: null,
        startDate: null,
        endDate: DateTime.UtcNow.AddYears(-1),
        pageNumber: 1,
        pageSize: 10000,
        default);
    
    // Archive to cold storage (blob, S3, etc.)
    await ArchiveToStorage(oldLogs.Logs);
    
    // Delete from database
    await _auditLogRepository.DeleteOlderThanAsync(
        DateTime.UtcNow.AddYears(-1), 
        default);
    await _unitOfWork.SaveChangesAsync(default);
}
```

---

## Performance Tips

### Audit Logs

- Use `SearchAsync` with date ranges to limit result sets
- Index on `Timestamp`, `EntityName`, `UserId`, `IpAddress` columns
- Archive old logs regularly (90+ days)
- Consider partitioning by date for large tables

### Refresh Tokens

- Clean expired tokens daily
- Monitor active session counts
- Set reasonable expiration times (7-30 days)
- Consider caching active token counts

### Email Service

- Use bulk operations for multiple recipients
- Implement retry logic for transient failures
- Queue emails for better performance
- Monitor SendGrid quota/usage

---

## Security Best Practices

✅ **Audit Logs**: Never delete logs for compliance periods (retain 90+ days minimum)  
✅ **Token Cleanup**: Remove expired/revoked tokens regularly  
✅ **Email Alerts**: Notify users of security-critical actions  
✅ **Session Limits**: Enforce maximum concurrent sessions  
✅ **IP Monitoring**: Track and alert on unusual IP patterns  
✅ **2FA Notifications**: Always notify when 2FA is changed  

---

## Complete Implementation Example

### User Management Command Handler

```csharp
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRefreshTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        
        // Track changes for audit
        var changes = new List<string>();
        if (user.Email != request.Email)
            changes.Add($"Email: {user.Email} → {request.Email}");
        
        // Update user
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        await _userRepository.UpdateAsync(user, ct);
        
        // Create audit log
        await _auditLogRepository.AddAsync(new AuditLog
        {
            EntityName = "User",
            EntityId = user.Id.ToString(),
            Action = "Update",
            Changes = string.Join(", ", changes),
            UserId = request.CurrentUserId.ToString(),
            IpAddress = request.IpAddress,
            Timestamp = DateTime.UtcNow
        }, ct);
        
        await _unitOfWork.SaveChangesAsync(ct);
        
        // Send notification if email changed
        if (changes.Any(c => c.StartsWith("Email")))
        {
            await _emailService.SendEmailAsync(
                request.Email,
                user.FirstName,
                "Email Address Updated",
                "Your email address has been successfully updated.",
                ct);
        }
        
        return _mapper.Map<UserDto>(user);
    }
}
```

For complete method signatures, see the interface definitions in:

- [IAuditLogRepository.cs](../src/Enterprise.Application/Common/Interfaces/IAuditLogRepository.cs)
- [IRefreshTokenRepository.cs](../src/Enterprise.Application/Common/Interfaces/IRefreshTokenRepository.cs)
- [IEmailService.cs](../src/Enterprise.Application/Common/Interfaces/IEmailService.cs)
