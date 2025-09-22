using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Base class for smoke tests that validates critical functionality across different configurations
/// Designed for fast execution (30-second max per configuration) as deployment validation
/// </summary>
public abstract class SmokeTestBase : IDisposable
{
    private readonly List<WebApplicationFactory<Api.Program>> _factories = new();

    /// <summary>
    /// Creates a test application factory for the specified environment configuration
    /// </summary>
    protected WebApplicationFactory<Api.Program> CreateFactoryForEnvironment(string environment)
    {
        var factory = new WebApplicationFactory<Api.Program>()
          .WithWebHostBuilder(builder =>
          {
              builder.UseEnvironment(environment);
              builder.ConfigureAppConfiguration((context, config) =>
          {
                config.Sources.Clear();

              // Environment-specific configuration
                var settings = environment switch
                {
                    "Development" => new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudSmoke_Dev_{Guid.NewGuid():N}.db",
                        ["DatabaseProvider"] = "SQLite",
                        ["Logging:LogLevel:Default"] = "Information",
                        ["Caching:UseRedis"] = "false",
                        ["OutputCaching:Disabled"] = "false"
                    },
                    "Testing" => new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudSmoke_Test_{Guid.NewGuid():N}.db",
                        ["DatabaseProvider"] = "SQLite",
                        ["Logging:LogLevel:Default"] = "Warning",
                        ["Caching:UseRedis"] = "false",
                        ["OutputCaching:Disabled"] = "false"
                    },
                    "Production" => new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudSmoke_Prod_{Guid.NewGuid():N}.db",
                        ["DatabaseProvider"] = "SQLite", // Using SQLite for smoke tests even in "Production" config
                        ["Logging:LogLevel:Default"] = "Error",
                        ["Caching:UseRedis"] = "false",
                        ["OutputCaching:Disabled"] = "false"
                    },
                    _ => throw new ArgumentException($"Unsupported environment: {environment}")
                };

                config.AddInMemoryCollection(settings);
            });

              // Reduce logging noise in smoke tests
              builder.ConfigureLogging(logging =>
          {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
          });

        _factories.Add(factory);
        return factory;
    }

    /// <summary>
    /// Executes an action with performance measurement and validation
    /// </summary>
    protected async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action, string operationName = "Operation")
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        // Log timing for monitoring (but don't fail the test based on timing alone in base class)
        if (stopwatch.Elapsed.TotalSeconds > 10) // Warning threshold
        {
            // This could be logged but we'll use console for now
            Console.WriteLine($"WARNING: {operationName} took {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Gets all supported environments for smoke testing
    /// </summary>
    protected static IEnumerable<string> GetSupportedEnvironments()
    {
        return new[] { "Development", "Testing", "Production" };
    }

    /// <summary>
    /// Gets supported environments as test data for xUnit Theory tests
    /// </summary>
    public static IEnumerable<object[]> GetEnvironmentsAsTestData()
    {
        return GetSupportedEnvironments().Select(env => new object[] { env });
    }

    /// <summary>
    /// Validates that a response indicates healthy status
    /// </summary>
    protected static void ValidateHealthyResponse(HttpResponseMessage response, string environment, string endpoint)
    {
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
          $"{endpoint} should return 200 OK in {environment} environment");

        response.Content.Headers.ContentType?.MediaType.Should().NotBeNullOrEmpty(
          $"{endpoint} should return content with media type in {environment} environment");
    }

    /// <summary>
    /// Creates an HTTP client for the specified environment with proper configuration
    /// </summary>
    protected HttpClient CreateClientForEnvironment(string environment)
    {
        var factory = CreateFactoryForEnvironment(environment);
        var client = factory.CreateClient();

        // Set a reasonable timeout for smoke tests
        client.Timeout = TimeSpan.FromSeconds(30);

        return client;
    }

    public void Dispose()
    {
        foreach (var factory in _factories)
        {
            factory.Dispose();
        }
        _factories.Clear();
    }
}
