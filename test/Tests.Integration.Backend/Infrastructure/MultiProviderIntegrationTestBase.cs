using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Base class for multi-provider integration tests
/// Allows running the same test logic across different database providers
/// </summary>
public abstract class MultiProviderIntegrationTestBase : IDisposable
{
    protected readonly HttpClient Client;
    protected readonly IMultiProviderTestWebApplicationFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected MultiProviderIntegrationTestBase(DatabaseProvider provider)
    {
        Factory = MultiProviderTestWebApplicationFactoryProvider.Create(provider);

        // Ensure database is created before creating client
        Factory.EnsureDatabaseCreated();

        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Gets the current database provider being tested
    /// </summary>
    protected DatabaseProvider Provider => Factory.Provider;

    /// <summary>
    /// Gets a human-readable provider name for test output
    /// </summary>
    public string ProviderName => Factory.ProviderName;

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// </summary>
    public async Task RunWithCleanDatabaseAsync(Func<Task> testAction)
    {
        // Clear database before test
        await Factory.ClearDatabaseAsync();

        try
        {
            await testAction();
        }
        finally
        {
            // Clean up after test to ensure isolation
            await Factory.ClearDatabaseAsync();
        }
    }

    /// <summary>
    /// Performs authenticated GET request with admin user
    /// </summary>
    public async Task<HttpResponseMessage> AuthenticatedGetAsync(string url)
    {
        await EnsureAuthenticatedAsync();
        return await Client.GetAsync(url);
    }

    /// <summary>
    /// Performs authenticated POST request with JSON payload
    /// </summary>
    public async Task<HttpResponseMessage> AuthenticatedPostJsonAsync<T>(string url, T data)
    {
        await EnsureAuthenticatedAsync();
        return await Client.PostAsJsonAsync(url, data, JsonOptions);
    }

    /// <summary>
    /// Performs authenticated PUT request with JSON payload
    /// </summary>
    public async Task<HttpResponseMessage> AuthenticatedPutJsonAsync<T>(string url, T data)
    {
        await EnsureAuthenticatedAsync();
        return await Client.PutAsJsonAsync(url, data, JsonOptions);
    }

    /// <summary>
    /// Performs authenticated DELETE request
    /// </summary>
    public async Task<HttpResponseMessage> AuthenticatedDeleteAsync(string url)
    {
        await EnsureAuthenticatedAsync();
        return await Client.DeleteAsync(url);
    }

    /// <summary>
    /// Reads JSON response content as specified type
    /// </summary>
    public async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    /// <summary>
    /// Ensures the HTTP client is authenticated with admin user
    /// </summary>
    private async Task EnsureAuthenticatedAsync()
    {
        if (Client.DefaultRequestHeaders.Authorization != null)
return;

        // Use the same authentication pattern as smoke tests which works correctly
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        var email = $"test_{Provider}_{guidPart}@example.com";
        var password = "Test123!@#";

        // Register user through API
        var registerCommand = new App.Features.Authentication.RegisterUserCommand
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "Admin"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerCommand);
        
        // Handle the case where user already exists (409 Conflict)
        if (registerResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // User already exists, that's fine for testing - we'll just login
        }
        else
        {
            // For any other response, ensure it was successful
            registerResponse.EnsureSuccessStatusCode();
        }

        // Update the user's role in the database
        await Factory.SetUserRoleAsync(email, "Admin");

        // CRITICAL: Login again to get a fresh JWT token with the updated roles
        // The previous token only contains the default "User" role
        var loginCommand = new App.Features.Authentication.LoginCommand
        {
            Email = email,
            Password = password
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);
        loginResponse.EnsureSuccessStatusCode();

        var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse!.AccessToken);
    }

    /// <summary>
    /// Creates test data specific to the current provider for isolation
    /// </summary>
    public string CreateProviderSpecificTestData(string baseName)
    {
        var fullName = $"{baseName}_{ProviderName}_{Guid.NewGuid():N}";
        return fullName.Length > 50 ? fullName[..50] : fullName;
    }

    public void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}
