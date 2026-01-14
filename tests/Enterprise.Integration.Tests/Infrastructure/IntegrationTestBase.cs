using Newtonsoft.Json;

namespace Enterprise.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests providing common setup and utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        // Create client with specific options to avoid PipeWriter issue in tests
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    /// <summary>
    /// Deserializes HTTP response content using Newtonsoft.Json (matches API serialization)
    /// </summary>
    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
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
