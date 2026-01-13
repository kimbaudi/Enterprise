using MediatR;

namespace EnterpriseApi.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Username, string Password, string IpAddress = "Unknown") : IRequest<LoginResponse>;
