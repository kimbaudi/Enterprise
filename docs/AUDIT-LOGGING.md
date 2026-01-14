# Audit Logging System - Implementation Guide

## Overview

The audit logging system automatically tracks all data-modifying operations (commands) in your application, providing a complete audit trail for compliance, security, and debugging purposes.

## ✅ Components Implemented

### 1. Domain Layer

**AuditLog Entity** (`Domain/Entities/AuditLog.cs`)

- Captures who did what, when, and from where
- Stores before/after values for data changes
- Indexed for efficient querying

### 2. Repository Layer

**IAuditLogRepository Interface** (`Domain/Interfaces/IAuditLogRepository.cs`)

- Defines audit log data operations

**AuditLogRepository Implementation** (`Infrastructure/Repositories/AuditLogRepository.cs`)

- Implements filtering by entity, user, action, date range
- Supports pagination for large audit logs
- Uses AsNoTracking for read performance

### 3. Application Layer

**AuditLoggingBehavior Pipeline** (`Application/Common/Behaviors/AuditLoggingBehavior.cs`)

- **Automatically intercepts all commands** via MediatR pipeline
- Captures request/response data as JSON
- Extracts entity information intelligently
- Non-blocking: won't fail requests if audit logging fails

**Audit Log Queries:**

- `GetAuditLogsQuery` - Get all audit logs with optional filters
- `GetAuditLogsByEntityQuery` - Track changes to specific entity type/ID
- `GetAuditLogsByUserQuery` - View all actions by a specific user

**AuditLogDto** (`Application/DTOs/AuditLogDto.cs`)

- Data transfer object for API responses

### 4. API Layer

**AuditLogsController** (`WebApi/Controllers/AuditLogsController.cs`)

- **Admin-only access** (`[Authorize(Roles = "Admin")]`)
- Three endpoints for querying audit logs
- Supports pagination and filtering

## 🔐 Security Features

1. **Role-Based Access**: Only administrators can view audit logs
2. **IP Address Tracking**: Records IP address of user making changes
3. **Tamper-Proof**: Audit logs are write-only (no update/delete endpoints)
4. **Indexed Database**: Fast queries on timestamp and entity name

## 📊 What Gets Logged

The system automatically logs:

- ✅ **Create operations** - New records with full data
- ✅ **Update operations** - Before/after values
- ✅ **Delete operations** - Deleted record data
- ✅ **User context** - UserId, Username, IP Address
- ✅ **Timestamp** - UTC time of operation
- ✅ **Entity details** - Entity name and ID

**Example logged actions:**

- CreateProduct → "Create" on "Product" entity
- UpdateUser → "Update" on "User" entity
- DeleteProduct → "Delete" on "Product" entity

## 🚀 API Endpoints

### Get All Audit Logs

```http
GET /api/v1/auditlogs?pageNumber=1&pageSize=10&action=Create&startDate=2026-01-01&endDate=2026-12-31
Authorization: Bearer {admin-token}
```

**Query Parameters:**

- `pageNumber` (default: 1)
- `pageSize` (default: 10)
- `action` (optional): Filter by Create, Update, Delete, etc.
- `startDate` (optional): Start date for range filter
- `endDate` (optional): End date for range filter

**Response:**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "userId": "user-guid",
        "username": "admin",
        "action": "Create",
        "entityName": "Product",
        "entityId": "product-guid",
        "oldValues": null,
        "newValues": "{\"name\":\"New Product\",\"price\":99.99}",
        "ipAddress": "192.168.1.1",
        "timestamp": "2026-01-14T10:30:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

### Get Audit Logs by Entity

```http
GET /api/v1/auditlogs/entity/Product?entityId=123&pageNumber=1&pageSize=10
Authorization: Bearer {admin-token}
```

**Use Case**: Track all changes to a specific product or all products

### Get Audit Logs by User

```http
GET /api/v1/auditlogs/user/{userId}?pageNumber=1&pageSize=10
Authorization: Bearer {admin-token}
```

**Use Case**: Review all actions performed by a specific user

## 🔧 Configuration

### Pipeline Order (Critical!)

In `Application/DependencyInjection.cs`, the AuditLoggingBehavior is registered **after** validation:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));
```

**Why?** Only valid commands should be audited. Invalid commands are rejected by validation.

### Database Schema

**AuditLogs Table:**

```sql
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450),
    Username NVARCHAR(50),
    Action NVARCHAR(50) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(450),
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IpAddress NVARCHAR(50),
    Timestamp DATETIME2 NOT NULL,
    INDEX IX_AuditLogs_Timestamp,
    INDEX IX_AuditLogs_EntityName
);
```

## 📝 Usage Examples

### Testing Audit Logging

1. **Login as Admin:**

```bash
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

1. **Create a Product (generates audit log):**

```bash
POST /api/v1/products
Authorization: Bearer {token}
{
  "name": "Test Product",
  "description": "Testing audit logging",
  "price": 99.99,
  "stock": 10,
  "category": "Electronics",
  "sku": "TEST-001"
}
```

1. **View Audit Logs:**

```bash
GET /api/v1/auditlogs
Authorization: Bearer {admin-token}
```

### Programmatic Access

```csharp
// In a query handler or service
public class SomeQueryHandler
{
    private readonly IAuditLogRepository _auditLogRepository;

    public async Task<List<AuditLog>> GetRecentChanges()
    {
        var logs = await _auditLogRepository.GetByDateRangeAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            pageNumber: 1,
            pageSize: 50);
        
        return logs.ToList();
    }
}
```

## 🎯 Best Practices

### 1. **Sensitive Data Handling**

The system serializes entire command objects. If you have sensitive data (passwords, credit cards):

```csharp
// Mark properties with [JsonIgnore] attribute
public record UpdateUserCommand(
    Guid Id,
    string Username,
    [property: JsonIgnore] string? Password  // Won't be logged
) : IRequest<UserDto>;
```

### 2. **Custom Action Names**

The behavior automatically determines actions from command names, but you can customize:

```csharp
// In AuditLoggingBehavior.cs, add custom mappings:
if (requestName.Contains("Approve")) return "Approve";
if (requestName.Contains("SendEmail")) return "EmailSent";
```

### 3. **Retention Policy**

Implement a cleanup job for old audit logs:

```csharp
// In a Hangfire job
public class AuditLogCleanupJob
{
    public async Task CleanupOldLogsAsync()
    {
        // Delete logs older than 1 year
        await _context.AuditLogs
            .Where(a => a.Timestamp < DateTime.UtcNow.AddYears(-1))
            .ExecuteDeleteAsync();
    }
}
```

### 4. **Performance Considerations**

- Audit logging uses AsNoTracking for reads
- Writes are async and won't block commands
- Failed audit writes are logged but don't fail requests
- Database indexes optimize common queries

## 🔍 Compliance Use Cases

### GDPR - Right to be Forgotten

```csharp
// Find all actions by user
GET /api/v1/auditlogs/user/{userId}

// Export for user request
var logs = await _auditLogRepository.GetByUserAsync(userId, 1, 1000);
```

### SOC 2 - Access Control Audit

```csharp
// Track who accessed sensitive data
GET /api/v1/auditlogs/entity/User?action=Update

// Review admin actions
GET /api/v1/auditlogs?startDate=2026-01-01&endDate=2026-01-31
```

### HIPAA - Patient Record Access

```csharp
// Track all changes to patient records
GET /api/v1/auditlogs/entity/Patient?entityId={patientId}
```

## 🐛 Troubleshooting

### Audit Logs Not Appearing

1. **Check Pipeline Registration:**

```csharp
// In Application/DependencyInjection.cs
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));
```

1. **Verify Command Naming:**
Commands must end with "Command" (e.g., `CreateProductCommand`)

2. **Check User Context:**
Ensure `ICurrentUserService` is returning user information

3. **View Logs:**
Check Serilog logs for audit logging failures:

```bash
tail -f logs/log-*.txt | grep "Failed to create audit log"
```

### Performance Issues

If audit logging slows down your API:

1. Reduce serialization depth (custom JSON serializer options)
2. Use background queue for audit writes (Hangfire)
3. Archive old logs to separate table/database

## 📈 Future Enhancements

Potential improvements:

- [ ] Audit log export to CSV/PDF
- [ ] Real-time audit log dashboard
- [ ] Anomaly detection (unusual patterns)
- [ ] Rollback functionality (restore from audit log)
- [ ] Blockchain-based audit trail
- [ ] Automatic alerts for sensitive operations

## 🧪 Testing

See audit log tests in:

- `tests/Enterprise.Application.Tests/Features/AuditLogs/`

Run tests:

```bash
dotnet test --filter "FullyQualifiedName~AuditLog"
```

## 📚 Related Documentation

- [CQRS Architecture](./CQRS-ARCHITECTURE.md)
- [Authentication Guide](./AUTHENTICATION.md)
- [Security Configuration](./SECURITY-CONFIGURATION.md)

---

**Status:** ✅ Fully Implemented and Tested
**Migration:** `20260114182332_AddAuditLogSystem.cs`
**Last Updated:** January 14, 2026
