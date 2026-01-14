using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Auth.Queries.Get2FAStatus;

public record Get2FAStatusQuery(Guid UserId) : IRequest<TwoFactorStatusResponse>;
