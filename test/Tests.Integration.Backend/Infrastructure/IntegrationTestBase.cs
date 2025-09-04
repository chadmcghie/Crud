using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// Helper method to assert successful response and provide detailed error information on failure
    /// </summary>
    protected async Task<HttpResponseMessage> AssertSuccessfulResponseAsync(HttpResponseMessage response, string requestDescription = "request")
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = $"{requestDescription} failed with status {response.StatusCode}";

            if (!string.IsNullOrEmpty(errorContent))
            {
                errorMessage += $"\nError Response: {errorContent}";
            }

            if (response.Headers.Any())
            {
                errorMessage += $"\nResponse Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}";
            }

            throw new InvalidOperationException(errorMessage);
        }

        return response;
    }

    /// <summary>
    /// Helper method to log detailed error information from HTTP response
    /// </summary>
    protected async Task LogErrorResponseAsync(HttpResponseMessage response, string context = "HTTP request")
    {
        var errorContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"❌ {context} failed:");
        Console.WriteLine($"   Status: {response.StatusCode} ({(int)response.StatusCode})");
        Console.WriteLine($"   Reason: {response.ReasonPhrase}");

        if (!string.IsNullOrEmpty(errorContent))
        {
            Console.WriteLine($"   Response Body: {errorContent}");
        }

        if (response.Headers.Any())
        {
            Console.WriteLine($"   Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
        }

        if (response.Content.Headers.Any())
        {
            Console.WriteLine($"   Content Headers: {string.Join(", ", response.Content.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
        }

        // Also dump server-side logs if available
        if (Factory.LogCapture?.HasErrors == true)
        {
            Console.WriteLine($"   🔍 Server-side errors detected:");
            Factory.LogCapture.DumpLogsToConsole(Microsoft.Extensions.Logging.LogLevel.Error);
        }
    }

    /// <summary>
    /// Enhanced PostJson method with automatic error logging
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonWithErrorLoggingAsync<T>(string requestUri, T data, bool throwOnError = false)
    {
        var response = await PostJsonAsync(requestUri, data);

        if (!response.IsSuccessStatusCode)
        {
            await LogErrorResponseAsync(response, $"POST {requestUri}");

            if (throwOnError)
            {
                await AssertSuccessfulResponseAsync(response, $"POST {requestUri}");
            }
        }

        return response;
    }

    /// <summary>
    /// Enhanced GET method with automatic error logging
    /// </summary>
    protected async Task<HttpResponseMessage> GetWithErrorLoggingAsync(string requestUri, bool throwOnError = false)
    {
        var response = await Client.GetAsync(requestUri);

        if (!response.IsSuccessStatusCode)
        {
            await LogErrorResponseAsync(response, $"GET {requestUri}");

            if (throwOnError)
            {
                await AssertSuccessfulResponseAsync(response, $"GET {requestUri}");
            }
        }

        return response;
    }

    public void Dispose()
    {
        Scope?.Dispose();
        // Note: Factory is disposed by the fixture, not here
    }
}

