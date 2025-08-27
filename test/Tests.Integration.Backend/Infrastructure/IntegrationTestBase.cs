using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Base class for integration tests with infrastructure abstraction
/// Uses dependency injection to decouple from specific database implementations
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactoryFixture>, IDisposable
{
    protected readonly HttpClient Client;
    protected readonly ITestWebApplicationFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected IntegrationTestBase(TestWebApplicationFactoryFixture fixture)
    {
        Factory = fixture.Factory;
        
        // Ensure database is created before creating client
        Factory.EnsureDatabaseCreated();
        
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// This is more reliable than transaction rollback for HTTP-based integration tests
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
            // Clear database after test for next test
            await Factory.ClearDatabaseAsync();
        }
    }

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// This is more reliable than transaction rollback for HTTP-based integration tests
    /// </summary>
    protected async Task<T> RunWithCleanDatabaseAsync<T>(Func<Task<T>> testAction)
    {
        // Clear database before test
        await Factory.ClearDatabaseAsync();
        
        try
        {
            return await testAction();
        }
        finally
        {
            // Clear database after test for next test
            await Factory.ClearDatabaseAsync();
        }
    }

    /// <summary>
    /// Legacy method for tests that need database clearing (prefer RunWithCleanDatabaseAsync)
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await Factory.ClearDatabaseAsync();
    }

    /// <summary>
    /// Helper method to post JSON data
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T data)
    {
        return await Client.PostAsJsonAsync(requestUri, data, JsonOptions);
    }

    /// <summary>
    /// Helper method to put JSON data
    /// </summary>
    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T data)
    {
        return await Client.PutAsJsonAsync(requestUri, data, JsonOptions);
    }

    /// <summary>
    /// Helper method to read JSON response
    /// </summary>
    protected async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public void Dispose()
    {
        Scope?.Dispose();
        // Note: Factory is disposed by the fixture, not here
    }
}

