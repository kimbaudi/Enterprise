using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    // Basic lookups
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    // Existence checks
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    // Role-based queries
    Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    // Status queries
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default);

    // 2FA queries
    Task<IEnumerable<User>> GetUsersWithTwoFactorEnabledAsync(CancellationToken cancellationToken = default);
    Task<int> CountUsersWithTwoFactorAsync(CancellationToken cancellationToken = default);

    // Search and pagination
    Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(
        string? searchTerm,
        bool? isActive,
        bool? twoFactorEnabled,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Batch operations
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetRecentlyCreatedUsersAsync(int count, CancellationToken cancellationToken = default);
}
