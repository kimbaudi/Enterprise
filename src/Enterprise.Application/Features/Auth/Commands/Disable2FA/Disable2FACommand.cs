using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Disable2FA;

public record Disable2FACommand(Guid UserId, string Code) : IRequest<bool>;
