using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EnterpriseApi.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // TODO: Replace with actual user validation against database
        // This is a demo implementation - in production, validate against your user store
        if (!ValidateCredentials(request.Username, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var token = GenerateJwtToken(request.Username);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        var response = new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            Username = request.Username
        };

        return Task.FromResult(response);
    }

    private bool ValidateCredentials(string username, string password)
    {
        // TODO: Replace with actual database lookup and password hash verification
        // For demo purposes, accepting any non-empty credentials
        // In production:
        // 1. Query user from database by username
        // 2. Verify password hash using BCrypt, PBKDF2, or similar
        // 3. Check if account is active, not locked, etc.
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        // Demo: Accept username "admin" with password "password"
        // Remove this in production!
        return username.Equals("admin", StringComparison.OrdinalIgnoreCase) && 
               password == "password";
    }

    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForJWTTokenGeneration123456";
        var issuer = jwtSettings["Issuer"] ?? "EnterpriseAPI";
        var audience = jwtSettings["Audience"] ?? "EnterpriseAPIUsers";
        var expirationHours = GetTokenExpirationHours();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Name, username),
            // Add additional claims as needed (roles, permissions, etc.)
            // new Claim(ClaimTypes.Role, "Admin"),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
