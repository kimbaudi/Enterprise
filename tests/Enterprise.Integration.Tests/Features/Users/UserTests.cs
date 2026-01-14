using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Integration.Tests.Infrastructure;
using Enterprise.WebApi.Common;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Enterprise.Integration.Tests.Features.Users;

/// <summary>
/// Integration tests for user management endpoints
/// </summary>
public class UserTests : IntegrationTestBase
{
    public UserTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_ReturnsUserList()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<UserDto>>>(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUsers_WithRegularUser_ReturnsForbidden()
    {
        // Arrange
        var client = await GetAuthenticatedUserClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/users?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_WithAdminRole_ReturnsUser()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Test user

        // Act
        var response = await client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
        result.Data.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserById_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_WithAdminRole_CreatesUserSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var newUser = new
        {
            Username = "integrationtestuser",
            Email = "integrationtest@test.com",
            Password = "IntegrationTest@123",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be(newUser.Username);
        result.Data.Email.Should().Be(newUser.Email);
    }

    [Fact]
    public async Task CreateUser_WithRegularUser_ReturnsForbidden()
    {
        // Arrange
        var client = await GetAuthenticatedUserClientAsync();
        var newUser = new
        {
            Username = "shouldnotcreate",
            Email = "shouldnot@test.com",
            Password = "Test@123",
            FirstName = "Should",
            LastName = "NotCreate"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_WithAdminRole_UpdatesUserSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Test user

        var updateUser = new
        {
            Id = userId,
            FirstName = "Updated",
            LastName = "TestUser",
            Email = "updated@test.com",
            IsActive = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/users/{userId}", updateUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FirstName.Should().Be(updateUser.FirstName);
        result.Data.LastName.Should().Be(updateUser.LastName);
    }

    [Fact]
    public async Task UpdateUser_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var nonExistentId = Guid.NewGuid();

        var updateUser = new
        {
            Id = nonExistentId,
            FirstName = "Update",
            LastName = "User",
            Email = "update@test.com",
            IsActive = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/users/{nonExistentId}", updateUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithAdminRole_DeletesSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // First, create a user to delete
        var newUser = new
        {
            Username = "usertodelete",
            Email = "todelete@test.com",
            Password = "ToDelete@123",
            FirstName = "To",
            LastName = "Delete"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", newUser);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        var userId = createResult!.Data!.Id;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is deleted
        var getResponse = await client.GetAsync($"/api/v1/users/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithRegularUser_ReturnsForbidden()
    {
        // Arrange
        var client = await GetAuthenticatedUserClientAsync();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Admin user

        // Act
        var response = await client.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users?pageNumber=1&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<UserDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUsers_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users?searchTerm=testadmin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<UserDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Items.Should().Contain(u => u.Username.Contains("testadmin"));
    }

    [Fact]
    public async Task GetUsers_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users?isActive=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<UserDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Items.Should().OnlyContain(u => u.IsActive);
    }
}
