using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<UserDto>;
