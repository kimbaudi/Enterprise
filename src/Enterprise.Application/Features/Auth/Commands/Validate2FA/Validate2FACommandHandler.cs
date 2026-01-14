using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using MediatR;
using System.Text.Json;

namespace Enterprise.Application.Features.Auth.Commands.Validate2FA;

public class Validate2FACommandHandler : IRequestHandler<Validate2FACommand, Validate2FAResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public Validate2FACommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITwoFactorService twoFactorService,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _twoFactorService = twoFactorService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Validate2FAResponse> Handle(Validate2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled");
        }

        // First check if it's a valid TOTP code
        bool isValid = _twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code);

        // If not valid TOTP, check if it's a recovery code
        if (!isValid && !string.IsNullOrEmpty(user.RecoveryCodes))
        {
            isValid = await ValidateAndConsumeRecoveryCode(user, request.Code, cancellationToken);
        }

        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid verification code");
        }

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, request.IpAddress);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new Validate2FAResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresAt = _jwtTokenService.GetTokenExpirationTime(),
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    private async Task<bool> ValidateAndConsumeRecoveryCode(Domain.Entities.User user, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.RecoveryCodes))
            return false;

        try
        {
            var hashedCodes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes);
            if (hashedCodes == null || hashedCodes.Count == 0)
                return false;

            // Check if the code matches any stored hash
            var matchingHash = hashedCodes.FirstOrDefault(hash => _twoFactorService.VerifyRecoveryCode(code, hash));

            if (matchingHash != null)
            {
                // Remove the used recovery code
                hashedCodes.Remove(matchingHash);
                user.RecoveryCodes = JsonSerializer.Serialize(hashedCodes);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
        catch
        {
            // Invalid JSON or other error
        }

        return false;
    }
}
