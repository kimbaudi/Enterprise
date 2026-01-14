using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    // Paginated queries
    Task<IEnumerable<AuditLog>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string? entityId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Count queries
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByEntityAsync(string entityName, string? entityId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserAsync(string userId, CancellationToken cancellationToken = default);

    // Recent and specific queries
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByIpAddressAsync(string ipAddress, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityName, string entityId, CancellationToken cancellationToken = default);

    // Advanced search
    Task<(IEnumerable<AuditLog> Logs, int TotalCount)> SearchAsync(
        string? entityName,
        string? entityId,
        string? userId,
        string? action,
        string? ipAddress,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Cleanup operations
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<int> DeleteByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
}
