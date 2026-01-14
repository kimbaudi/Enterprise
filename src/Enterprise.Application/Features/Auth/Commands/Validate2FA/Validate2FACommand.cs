using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Validate2FA;

public record Validate2FACommand(Guid UserId, string Code, string IpAddress = "Unknown") : IRequest<Validate2FAResponse>;
