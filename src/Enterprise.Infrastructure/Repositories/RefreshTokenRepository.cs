using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enterprise.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId &&
                         rt.RevokedAt == null &&
                         rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _dbSet
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        await DeleteRangeAsync(expiredTokens, cancellationToken);
    }
}
