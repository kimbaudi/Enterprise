using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordResponse>;

public record ResetPasswordResponse(string Message);
