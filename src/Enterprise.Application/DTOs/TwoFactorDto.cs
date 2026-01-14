namespace Enterprise.Application.DTOs;

public record Enable2FAResponse
{
    public string Secret { get; init; } = string.Empty;
    public string QrCodeUrl { get; init; } = string.Empty;
    public string ManualEntryKey { get; init; } = string.Empty;
}

public record Verify2FAResponse
{
    public bool IsVerified { get; init; }
    public List<string> RecoveryCodes { get; init; } = new();
}

public record Validate2FAResponse
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAt { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}

public record TwoFactorStatusResponse
{
    public bool IsEnabled { get; init; }
    public bool HasRecoveryCodes { get; init; }
}
