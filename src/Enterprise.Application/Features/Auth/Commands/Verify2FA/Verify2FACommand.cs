using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Verify2FA;

public record Verify2FACommand(Guid UserId, string Code) : IRequest<Verify2FAResponse>;
