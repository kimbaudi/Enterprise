using MediatR;

namespace EnterpriseApi.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string IpAddress = "Unknown") : IRequest<RegisterResponse>;
