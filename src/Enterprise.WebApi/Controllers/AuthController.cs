using Asp.Versioning;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Auth.Commands.Disable2FA;
using Enterprise.Application.Features.Auth.Commands.Enable2FA;
using Enterprise.Application.Features.Auth.Commands.ForgotPassword;
using Enterprise.Application.Features.Auth.Commands.Login;
using Enterprise.Application.Features.Auth.Commands.RefreshToken;
using Enterprise.Application.Features.Auth.Commands.Register;
using Enterprise.Application.Features.Auth.Commands.ResetPassword;
using Enterprise.Application.Features.Auth.Commands.Validate2FA;
using Enterprise.Application.Features.Auth.Commands.Verify2FA;
using Enterprise.Application.Features.Auth.Queries.Get2FAStatus;
using Enterprise.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Enterprise.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("auth")] // Apply auth rate limit policy to entire controller
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="command">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration confirmation</returns>
    [HttpPost("register")]
    [DisableRateLimiting] // Override: Allow more registrations
    [EnableRateLimiting("api")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<RegisterResponse>(result));
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token with expiration information</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<LoginResponse>(result));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="command">Refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<RefreshTokenResponse>(result));
    }

    /// <summary>
    /// Request a password reset token
    /// </summary>
    /// <param name="command">Password reset request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("expensive")] // Very strict: 10 per 5 minutes
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<ForgotPasswordResponse>(result));
    }

    /// <summary>
    /// Reset password using token
    /// </summary>
    /// <param name="command">Password reset command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("reset-password")]
    [EnableRateLimiting("expensive")]
    public async Task<ActionResult<ApiResponse<ResetPasswordResponse>>> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<ResetPasswordResponse>(result));
    }

    /// <summary>
    /// Enable two-factor authentication for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>2FA setup response with QR code</returns>
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Enable2FAResponse>>> Enable2FA(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new Enable2FACommand(userId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<Enable2FAResponse>(result));
    }

    /// <summary>
    /// Verify two-factor authentication setup by validating the code
    /// </summary>
    /// <param name="request">Verification request with code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    [HttpPost("2fa/verify")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Verify2FAResponse>>> Verify2FA(
        [FromBody] Verify2FARequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new Verify2FACommand(userId, request.Code);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<Verify2FAResponse>(result));
    }

    /// <summary>
    /// Validate two-factor authentication code during login
    /// </summary>
    /// <param name="request">Validation request with user ID and code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with token</returns>
    [HttpPost("2fa/validate")]
    public async Task<ActionResult<ApiResponse<Validate2FAResponse>>> Validate2FA(
        [FromBody] Validate2FARequest request,
        CancellationToken cancellationToken)
    {
        var command = new Validate2FACommand(request.UserId, request.Code, GetClientIpAddress());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<Validate2FAResponse>(result));
    }

    /// <summary>
    /// Disable two-factor authentication for the current user
    /// </summary>
    /// <param name="request">Disable request with code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Disable2FA(
        [FromBody] Disable2FARequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new Disable2FACommand(userId, request.Code);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }

    /// <summary>
    /// Get two-factor authentication status for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>2FA status information</returns>
    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TwoFactorStatusResponse>>> Get2FAStatus(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = new Get2FAStatusQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<TwoFactorStatusResponse>(result));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class Verify2FARequest
{
    public string Code { get; set; } = string.Empty;
}

public class Validate2FARequest
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class Disable2FARequest
{
    public string Code { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
