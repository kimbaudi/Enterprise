using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityName,
        string? entityId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(a => a.EntityName == entityName);

        if (!string.IsNullOrEmpty(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(
        string action,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByEntityAsync(
        string entityName,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(a => a.EntityName == entityName);

        if (!string.IsNullOrEmpty(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .CountAsync(cancellationToken);
    }
}
