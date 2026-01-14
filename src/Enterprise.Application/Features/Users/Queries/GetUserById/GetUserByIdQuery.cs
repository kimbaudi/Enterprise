using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;
