using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// JWT authentication configuration with validation
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Secret key for JWT token signing (minimum 32 characters)
    /// Should be stored in User Secrets (development) or environment variables (production)
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer identifier
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience identifier
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration in hours (default: 24 hours)
    /// </summary>
    [Range(1, 720, ErrorMessage = "ExpirationHours must be between 1 and 720 (30 days)")]
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// Refresh token expiration in days (default: 7 days)
    /// </summary>
    [Range(1, 90, ErrorMessage = "RefreshTokenExpirationDays must be between 1 and 90")]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
