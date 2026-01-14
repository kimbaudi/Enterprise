using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Enterprise.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;

    public LoginCommandHandler(
        IConfiguration configuration,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user from database with roles
        var user = await _userRepository.GetByUsernameWithRolesAsync(request.Username, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled. Please contact support.");
        }

        // Check if account is locked out
        if (user.IsLockedOut)
        {
            var remainingMinutes = (int)(user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes;
            throw new UnauthorizedAccessException($"Account is locked. Please try again in {remainingMinutes} minutes.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new UnauthorizedAccessException($"Account locked due to too many failed login attempts. Try again in {LockoutMinutes} minutes.");
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Reset failed login attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id, request.IpAddress);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        return new LoginResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    private int GetTokenExpirationHours()
    {
        var expirationHours = _configuration.GetSection("JwtSettings")["ExpirationHours"];
        if (int.TryParse(expirationHours, out var hours))
        {
            return hours;
        }
        return 24; // Default to 24 hours
    }
}
