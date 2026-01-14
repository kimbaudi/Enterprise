using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    // Token retrieval
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);

    // Token revocation
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    // Token cleanup
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task DeleteRevokedTokensAsync(CancellationToken cancellationToken = default);
    Task DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    // Token statistics
    Task<int> CountActiveTokensByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountTotalActiveTokensAsync(CancellationToken cancellationToken = default);
}
