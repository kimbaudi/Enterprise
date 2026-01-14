using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that automatically logs all command operations to the audit log.
/// Captures before/after state for tracking data changes.
/// </summary>
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLoggingBehavior<TRequest, TResponse>> _logger;

    public AuditLoggingBehavior(
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<AuditLoggingBehavior<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only audit commands (operations that modify data)
        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command"))
        {
            return await next();
        }

        // Skip audit logging commands to prevent infinite loops
        if (requestName.Contains("AuditLog"))
        {
            return await next();
        }

        var userId = _currentUserService.UserId;
        var username = _currentUserService.Username;
        var ipAddress = _currentUserService.IpAddress;

        // Serialize the request (before state)
        string oldValues;
        try
        {
            oldValues = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize request for audit logging");
            oldValues = "Unable to serialize";
        }

        // Execute the command
        var response = await next();

        // Serialize the response (after state)
        string newValues;
        try
        {
            newValues = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize response for audit logging");
            newValues = "Unable to serialize";
        }

        // Determine action and entity name from request name
        var action = DetermineAction(requestName);
        var entityName = ExtractEntityName(requestName);
        var entityId = ExtractEntityId(response);

        // Create audit log entry
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username ?? "Anonymous",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: User {Username} performed {Action} on {EntityName} (ID: {EntityId})",
                username, action, entityName, entityId);
        }
        catch (Exception ex)
        {
            // Don't fail the request if audit logging fails
            _logger.LogError(ex, "Failed to create audit log for {RequestName}", requestName);
        }

        return response;
    }

    private static string DetermineAction(string requestName)
    {
        if (requestName.Contains("Create")) return "Create";
        if (requestName.Contains("Update")) return "Update";
        if (requestName.Contains("Delete")) return "Delete";
        if (requestName.Contains("Import")) return "Import";
        if (requestName.Contains("Export")) return "Export";
        if (requestName.Contains("Approve")) return "Approve";
        if (requestName.Contains("Reject")) return "Reject";
        if (requestName.Contains("Activate")) return "Activate";
        if (requestName.Contains("Deactivate")) return "Deactivate";

        return "Execute";
    }

    private static string ExtractEntityName(string requestName)
    {
        // Remove "Command" suffix
        var name = requestName.Replace("Command", "");

        // Remove action prefixes
        name = name.Replace("Create", "")
                   .Replace("Update", "")
                   .Replace("Delete", "")
                   .Replace("Import", "")
                   .Replace("Export", "")
                   .Replace("Approve", "")
                   .Replace("Reject", "")
                   .Replace("Activate", "")
                   .Replace("Deactivate", "");

        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
    }

    private static string? ExtractEntityId(TResponse response)
    {
        if (response == null)
            return null;

        var type = response.GetType();

        // Try to find Id property
        var idProperty = type.GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(response);
            return value?.ToString();
        }

        // For nested responses (e.g., ApiResponse<T>)
        var dataProperty = type.GetProperty("Data");
        if (dataProperty != null)
        {
            var data = dataProperty.GetValue(response);
            if (data != null)
            {
                var nestedIdProperty = data.GetType().GetProperty("Id");
                if (nestedIdProperty != null)
                {
                    var value = nestedIdProperty.GetValue(data);
                    return value?.ToString();
                }
            }
        }

        return null;
    }
}
