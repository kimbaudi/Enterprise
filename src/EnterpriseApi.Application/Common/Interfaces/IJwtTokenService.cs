using EnterpriseApi.Domain.Entities;

namespace EnterpriseApi.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string ipAddress);
}
