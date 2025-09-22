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
    protected string ProviderName => Factory.ProviderName;

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// </summary>
    protected async Task RunWithCleanDatabaseAsync(Func<Task> testAction)
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
    protected async Task<HttpResponseMessage> AuthenticatedGetAsync(string url)
    {
        await EnsureAuthenticatedAsync();
        return await Client.GetAsync(url);
    }

    /// <summary>
    /// Performs authenticated POST request with JSON payload
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedPostJsonAsync<T>(string url, T data)
    {
        await EnsureAuthenticatedAsync();
        return await Client.PostAsJsonAsync(url, data, JsonOptions);
    }

    /// <summary>
    /// Performs authenticated PUT request with JSON payload
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedPutJsonAsync<T>(string url, T data)
    {
        await EnsureAuthenticatedAsync();
        return await Client.PutAsJsonAsync(url, data, JsonOptions);
    }

    /// <summary>
    /// Performs authenticated DELETE request
    /// </summary>
    protected async Task<HttpResponseMessage> AuthenticatedDeleteAsync(string url)
    {
        await EnsureAuthenticatedAsync();
        return await Client.DeleteAsync(url);
    }

    /// <summary>
    /// Reads JSON response content as specified type
    /// </summary>
    protected async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
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

        // Register a test user and get their token
        var email = $"test_{Provider}_{Guid.NewGuid():N[..8]}@example.com";
        var password = "Test123!@#";

        var token = await AuthenticationTestHelper.RegisterAndGetTokenAsync(Client, email, password);

        // Update user role to admin for multi-provider testing
        await Factory.SetUserRoleAsync(email, "Admin");

        // Get a fresh token with admin role
        var adminToken = await AuthenticationTestHelper.GetAuthTokenAsync(Client, email, password);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    }

    /// <summary>
    /// Creates test data specific to the current provider for isolation
    /// </summary>
    protected string CreateProviderSpecificTestData(string baseName)
    {
        return $"{baseName}_{ProviderName}_{Guid.NewGuid():N[..8]}";
    }

    public void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}