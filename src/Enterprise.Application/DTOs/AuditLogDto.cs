namespace Enterprise.Application.DTOs;

public record AuditLogDto(
    Guid Id,
    string? UserId,
    string Username,
    string Action,
    string EntityName,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime Timestamp
);
