namespace EnterpriseApi.Application.Features.Auth.Commands.Login;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
}
