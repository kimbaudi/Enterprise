using Enterprise.Application.Features.Auth.Commands.Login;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class LoginRequestExample : IExamplesProvider<LoginCommand>
{
    public LoginCommand GetExamples()
    {
        return new LoginCommand(
            Username: "admin",
            Password: "Admin@123",
            IpAddress: "192.168.1.1"
        );
    }
}
