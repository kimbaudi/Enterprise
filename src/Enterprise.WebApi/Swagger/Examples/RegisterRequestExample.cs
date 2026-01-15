using Enterprise.Application.Features.Auth.Commands.Register;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class RegisterRequestExample : IExamplesProvider<RegisterCommand>
{
    public RegisterCommand GetExamples()
    {
        return new RegisterCommand(
            Username: "johndoe",
            Email: "john.doe@example.com",
            Password: "SecureP@ssw0rd123",
            ConfirmPassword: "SecureP@ssw0rd123",
            FirstName: "John",
            LastName: "Doe"
        );
    }
}
