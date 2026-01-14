using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string? entityId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByEntityAsync(string entityName, string? entityId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserAsync(string userId, CancellationToken cancellationToken = default);
}
