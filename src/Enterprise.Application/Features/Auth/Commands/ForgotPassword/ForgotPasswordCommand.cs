using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;

public record ForgotPasswordResponse(string Message);
