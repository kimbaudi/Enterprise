using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Enable2FA;

public record Enable2FACommand(Guid UserId) : IRequest<Enable2FAResponse>;
