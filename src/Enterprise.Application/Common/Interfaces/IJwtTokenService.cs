using Enterprise.Domain.Entities;
using System.Security.Claims;

namespace Enterprise.Application.Common.Interfaces;

public interface IJwtTokenService
{
    // Token generation
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string ipAddress);
    DateTime GetTokenExpirationTime();

    // Token validation
    bool ValidateToken(string token);
    bool IsTokenExpired(string token);

    // Token parsing
    ClaimsPrincipal? GetPrincipalFromToken(string token);
    Guid? GetUserIdFromToken(string token);
    string? GetUsernameFromToken(string token);
    IEnumerable<string> GetRolesFromToken(string token);
    IEnumerable<Claim> GetClaimsFromToken(string token);

    // Token utilities
    DateTime? GetTokenExpirationDate(string token);
    TimeSpan GetRemainingTokenLifetime(string token);
}
