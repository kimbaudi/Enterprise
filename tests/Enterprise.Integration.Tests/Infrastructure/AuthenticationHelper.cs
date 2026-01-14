using Enterprise.Application.Features.Auth.Commands.Login;
using Enterprise.WebApi.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Enterprise.Integration.Tests.Infrastructure;

/// <summary>
/// Helper class for authentication in integration tests
/// </summary>
public static class AuthenticationHelper
{
    public static async Task<string> GetJwtTokenAsync(
        HttpClient client,
        string username = "testadmin",
        string password = "Admin@123")
    {
        var loginRequest = new
        {
            Username = username,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed with status {response.StatusCode}. Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return result?.Data?.Token ?? throw new InvalidOperationException("Token not received from login");
    }

    public static void SetBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<HttpClient> GetAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string username = "testadmin",
        string password = "Admin@123")
    {
        var client = factory.CreateClient();
        var token = await GetJwtTokenAsync(client, username, password);
        SetBearerToken(client, token);
        return client;
    }
}
