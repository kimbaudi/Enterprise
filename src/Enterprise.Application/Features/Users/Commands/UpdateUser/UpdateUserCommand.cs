using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive
) : IRequest<UserDto>;
