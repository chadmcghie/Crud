using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.Features.Authentication;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Helper class for handling authentication in integration tests
/// </summary>
public static class AuthenticationTestHelper
{
    /// <summary>
    /// Creates a test user and returns an authenticated HttpClient
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        ITestWebApplicationFactory factory,
        string role = "User",
        string? email = null,
        string? password = null)
    {
        // Ensure database is created before attempting authentication operations
        factory.EnsureDatabaseCreated();
        
        var client = factory.CreateClient();

        // Use provided credentials or generate unique ones for parallel test execution
        // Use GUID + timestamp to ensure uniqueness across parallel test runs
        email ??= $"test{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}@example.com";
        password ??= "Test123!@#";

        // If admin role is requested, we need to update the user's role BEFORE getting the token
        // This would typically be done through a separate admin endpoint
        if (role == "Admin")
        {
            // Register the user first
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Test",
                LastName = "Admin"
            };

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerCommand);
            
            // Handle the case where user already exists (409 Conflict)
            // In parallel test execution, multiple tests might try to register the same user
            if (registerResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // User already exists, that's fine for testing - we'll just login
                // This handles race conditions in parallel test execution
            }
            else
            {
                // For any other response, ensure it was successful
                registerResponse.EnsureSuccessStatusCode();
            }

            // Update the user's role in the database
            await factory.SetUserRoleAsync(email, "Admin");

            // CRITICAL: Login again to get a fresh JWT token with the updated roles
            // The previous token only contains the default "User" role
            var loginCommand = new LoginCommand
            {
                Email = email,
                Password = password
            };

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
            loginResponse.EnsureSuccessStatusCode();

            var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

            // Create a new client with the authentication token
            var authenticatedClient = factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);

            return authenticatedClient;
        }
        else
        {
            // Register the user
            var registerCommand = new RegisterUserCommand
            {
                Email = email,
                Password = password,
                FirstName = "Test",
                LastName = "User"
            };

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerCommand);
            
            // Handle the case where user already exists (409 Conflict)
            // In parallel test execution, multiple tests might try to register the same user
            if (registerResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // User already exists, that's fine for testing - we'll just login
                // This handles race conditions in parallel test execution
            }
            else
            {
                // For any other response, ensure it was successful
                registerResponse.EnsureSuccessStatusCode();
            }

            TokenResponse? tokenResponse = null;
            
            // If registration was successful, get token from registration response
            if (registerResponse.IsSuccessStatusCode)
            {
                tokenResponse = await registerResponse.Content.ReadFromJsonAsync<TokenResponse>();
            }
            else
            {
                // If registration failed (user exists), login to get token
                var loginCommand = new LoginCommand
                {
                    Email = email,
                    Password = password
                };

                var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
                loginResponse.EnsureSuccessStatusCode();
                tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
            }

            // Create a new client with the authentication token
            var authenticatedClient = factory.CreateClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);

            return authenticatedClient;
        }
    }

    /// <summary>
    /// Creates an admin user and returns an authenticated HttpClient
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(ITestWebApplicationFactory factory)
    {
        return await CreateAuthenticatedClientAsync(factory, "Admin");
    }

    /// <summary>
    /// Creates a regular user and returns an authenticated HttpClient
    /// </summary>
    public static async Task<HttpClient> CreateUserClientAsync(ITestWebApplicationFactory factory)
    {
        return await CreateAuthenticatedClientAsync(factory, "User");
    }

    /// <summary>
    /// Adds authentication header to an existing HttpClient
    /// </summary>
    public static async Task<string> GetAuthTokenAsync(HttpClient client, string email, string password)
    {
        var loginCommand = new LoginCommand
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse!.AccessToken!;
    }

    /// <summary>
    /// Registers a test user and returns the token
    /// </summary>
    public static async Task<string> RegisterAndGetTokenAsync(
        HttpClient client,
        string? email = null,
        string? password = null)
    {
        email ??= $"test-{Guid.NewGuid()}@example.com";
        password ??= "Test123!@#";

        var registerCommand = new RegisterUserCommand
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerCommand);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse!.AccessToken!;
    }
}

// Move TokenResponse to shared location if not already defined
public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}
