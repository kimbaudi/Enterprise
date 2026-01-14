using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Auth.Commands.ForgotPassword;
using Enterprise.Application.Features.Auth.Commands.Login;
using Enterprise.Application.Features.Auth.Commands.RefreshToken;
using Enterprise.Application.Features.Auth.Commands.Register;
using Enterprise.Application.Features.Auth.Commands.ResetPassword;
using Enterprise.Application.Features.Auth.Commands.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    /// <param name="request">Registration details</param>
    /// <returns>Registration confirmation</returns>
    [HttpPost("register")]
    [DisableRateLimiting] // Override: Allow more registrations
    [EnableRateLimiting("api")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<AuthResult>(result));
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token with expiration information</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<AuthResult>(result));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<AuthResult>(result));
    }

    [HttpPost("revoke-token")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeToken(
        [FromBody] RevokeTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }

    /// <summary>
    /// Request a password reset token
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("expensive")] // Very strict: 10 per 5 minutes
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<string>(result));
    }

    /// <summary>
    /// Reset password using token
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("expensive")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<string>(result));
    }
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
