using Enterprise.Application.Features.Auth.Commands.Login;
using Enterprise.Application.Features.Auth.Commands.Register;
using Enterprise.Integration.Tests.Infrastructure;
using Enterprise.WebApi.Common;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Enterprise.Integration.Tests.Features.Auth;

/// <summary>
/// Integration tests for authentication endpoints
/// </summary>
public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testadmin",
            Password = "Admin@123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.Username.Should().Be("testadmin");
        result.Data.Email.Should().Be("testadmin@test.com");
        result.Data.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testadmin",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // 401 responses return ProblemDetails, not ApiResponse
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "nonexistentuser",
            Password = "Password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var registerRequest = new
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "NewUser@123",
            ConfirmPassword = "NewUser@123",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("newuser");
        result.Data.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new
        {
            Username = "testadmin", // Already exists
            Email = "different@test.com",
            Password = "NewUser@123",
            ConfirmPassword = "NewUser@123",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new
        {
            Username = "validuser",
            Email = "invalid-email", // Invalid format
            Password = "ValidPass@123",
            ConfirmPassword = "ValidPass@123",
            FirstName = "Valid",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new
        {
            Username = "validuser",
            Email = "valid@test.com",
            Password = "weak", // Too weak
            ConfirmPassword = "weak",
            FirstName = "Valid",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await Client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_MultipleTimes_GeneratesDifferentTokens()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testadmin",
            Password = "Admin@123"
        };

        // Act
        var response1 = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        await Task.Delay(1000); // Wait to ensure different timestamps

        var response2 = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        // Assert
        result1!.Data!.Token.Should().NotBe(result2!.Data!.Token);
    }
}
