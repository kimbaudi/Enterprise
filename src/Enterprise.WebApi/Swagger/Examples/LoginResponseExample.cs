using Enterprise.Application.Features.Auth.Commands.Login;
using Enterprise.WebApi.Common;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class LoginResponseExample : IExamplesProvider<ApiResponse<LoginResponse>>
{
    public ApiResponse<LoginResponse> GetExamples()
    {
        return new ApiResponse<LoginResponse>(
            new LoginResponse
            {
                Username = "admin",
                Email = "admin@enterprise.com",
                FirstName = "Admin",
                LastName = "User",
                Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1NTBlODQwMC1lMjliLTQxZDQtYTcxNi00NDY2NTU0NDAwMDAiLCJ1bmlxdWVfbmFtZSI6ImFkbWluIiwiZW1haWwiOiJhZG1pbkBlbnRlcnByaXNlLmNvbSIsInJvbGUiOiJBZG1pbiIsIm5iZiI6MTcwNTI0MjAwMCwiZXhwIjoxNzA1MzI4NDAwLCJpYXQiOjE3MDUyNDIwMDB9.example_signature",
                TokenType = "Bearer",
                RefreshToken = "r3fr3sh_t0k3n_3x4mpl3_v4lu3_h3r3",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                Roles = new List<string> { "Admin" },
                RequiresTwoFactor = false
            }
        );
    }
}
