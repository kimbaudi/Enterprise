using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.BackgroundJobs;

public class DatabaseCleanupJob
{
    private readonly IRepository<RefreshToken> _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DatabaseCleanupJob> _logger;

    public DatabaseCleanupJob(
        IRepository<RefreshToken> tokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<DatabaseCleanupJob> logger)
    {
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task CleanupExpiredTokensAsync()
    {
        _logger.LogInformation("Starting expired token cleanup job");

        var expiredTokens = (await _tokenRepository.FindAsync(
            t => t.ExpiresAt < DateTime.UtcNow,
            CancellationToken.None)).ToList();

        if (expiredTokens.Count != 0)
        {
            foreach (var token in expiredTokens)
            {
                await _tokenRepository.DeleteAsync(token.Id, CancellationToken.None);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
        }
        else
        {
            _logger.LogInformation("No expired tokens to clean up");
        }
    }
}
