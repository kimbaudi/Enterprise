using Enterprise.Application.Features.Users.Commands.CreateUser;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class CreateUserRequestExample : IExamplesProvider<CreateUserCommand>
{
    public CreateUserCommand GetExamples()
    {
        return new CreateUserCommand(
            Username: "jane.smith",
            Email: "jane.smith@company.com",
            Password: "SecureP@ss2026!",
            FirstName: "Jane",
            LastName: "Smith"
        );
    }
}
