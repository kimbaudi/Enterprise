using System.Text.Json;

namespace Enterprise.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests providing common setup and utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        // Create client with HTTP/2 support
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        Client.DefaultRequestVersion = new Version(2, 0);
    }

    /// <summary>
    /// Deserializes HTTP response content using System.Text.Json with async serialization
    /// </summary>
    protected static async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
    }

    /// <summary>
    /// Gets an authenticated HTTP client with admin credentials
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedAdminClientAsync()
    {
        return await AuthenticationHelper.GetAuthenticatedClientAsync(Factory, "testadmin", "Admin@123");
    }

    /// <summary>
    /// Gets an authenticated HTTP client with regular user credentials
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedUserClientAsync()
    {
        return await AuthenticationHelper.GetAuthenticatedClientAsync(Factory, "testuser", "User@123");
    }

    /// <summary>
    /// Gets a JWT token for the specified user
    /// </summary>
    protected async Task<string> GetJwtTokenAsync(string username, string password)
    {
        return await AuthenticationHelper.GetJwtTokenAsync(Client, username, password);
    }
}
