using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Base class for contract tests that validates API contracts across different configurations
/// Ensures that configuration changes don't break existing API contracts, middleware behavior, or service interfaces
/// </summary>
public abstract class ContractTestBase : IDisposable
{
    private readonly List<WebApplicationFactory<Api.Program>> _factories = new();

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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

              // CRITICAL FIX: Load appsettings files first to get JWT and other configurations
              // Find the API project directory
              var currentDirectory = Directory.GetCurrentDirectory();
              var repoRoot = currentDirectory;
              while (!Directory.Exists(Path.Combine(repoRoot, "src")) && Directory.GetParent(repoRoot) != null)
              {
                  repoRoot = Directory.GetParent(repoRoot)!.FullName;
              }
              var apiConfigPath = Path.Combine(repoRoot, "src", "Api");

              // Load base configuration files (includes JWT settings)
              config.SetBasePath(apiConfigPath);
              config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
              config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
              config.AddEnvironmentVariables();

              // Override with test-specific settings for contract testing (these will override appsettings values)
              var contractTestOverrides = environment switch
              {
                  "Development" => new Dictionary<string, string?>
                  {
                      ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudContract_Deve_{Guid.NewGuid():N}.db",
                      ["DatabaseProvider"] = "SQLite",
                      ["Logging:LogLevel:Default"] = "Information",
                      ["Caching:UseRedis"] = "false",
                      ["OutputCaching:Disabled"] = "false"
                  },
                  "Testing" => new Dictionary<string, string?>
                  {
                      ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudContract_Test_{Guid.NewGuid():N}.db",
                      ["DatabaseProvider"] = "SQLite",
                      ["Logging:LogLevel:Default"] = "Warning",
                      ["Caching:UseRedis"] = "false",
                      ["OutputCaching:Disabled"] = "false"
                  },
                  "Production" => new Dictionary<string, string?>
                  {
                      ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudContract_Prod_{Guid.NewGuid():N}.db",
                      ["DatabaseProvider"] = "SQLite", // Using SQLite for contract tests even in "Production" config
                      ["Logging:LogLevel:Default"] = "Error",
                      ["Caching:UseRedis"] = "false",
                      ["OutputCaching:Disabled"] = "false"
                  },
                  _ => throw new ArgumentException($"Unsupported environment: {environment}")
              };

              // Add contract test overrides AFTER appsettings to ensure they take precedence
              config.AddInMemoryCollection(contractTestOverrides);
          });

              // Configure logging based on environment for contract tests
              builder.ConfigureLogging(logging =>
          {
              logging.ClearProviders();
              logging.AddConsole();
              
              // Set minimum level based on environment
              var minLevel = environment switch
              {
                  "Development" => LogLevel.Information,
                  "Testing" => LogLevel.Warning,
                  "Production" => LogLevel.Error,
                  _ => LogLevel.Warning
              };
              logging.SetMinimumLevel(minLevel);
          });
          });

        _factories.Add(factory);
        return factory;
    }

    /// <summary>
    /// Gets all supported environments for contract testing
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
    /// Creates an HTTP client for the specified environment with proper configuration
    /// </summary>
    protected HttpClient CreateClientForEnvironment(string environment)
    {
        var factory = CreateFactoryForEnvironment(environment);
        var client = factory.CreateClient();

        // Set a reasonable timeout for contract tests
        client.Timeout = TimeSpan.FromSeconds(30);

        return client;
    }

    /// <summary>
    /// Validates that a response has the expected contract structure
    /// </summary>
    protected static void ValidateResponseContract(HttpResponseMessage response, HttpStatusCode expectedStatusCode, string? expectedContentType = null)
    {
        response.StatusCode.Should().Be(expectedStatusCode,
          $"API contract should maintain consistent status codes across configurations");

        if (expectedContentType != null)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be(expectedContentType,
              $"API contract should maintain consistent content types across configurations");
        }
    }

    /// <summary>
    /// Deserializes JSON response and validates it against a contract type
    /// </summary>
    protected static async Task<T> ValidateJsonContract<T>(HttpResponseMessage response) where T : class
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
          "JSON endpoints should maintain application/json content type contract");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace("Response should contain valid JSON content");

        try
        {
            var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
            result.Should().NotBeNull($"Response should deserialize to {typeof(T).Name} contract");
            return result!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Response failed to deserialize to {typeof(T).Name} contract: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that a contract structure matches expected properties
    /// </summary>
    protected static void ValidateContractStructure<T>(T contract, Action<T> contractValidation) where T : class
    {
        contract.Should().NotBeNull("Contract object should not be null");
        contractValidation(contract);
    }

    /// <summary>
    /// Runs contract validation across all environments and ensures consistency
    /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
    protected async Task<Dictionary<string, T>> ValidateContractAcrossEnvironments<T>(
      string endpoint,
      Func<HttpClient, Task<HttpResponseMessage>> requestAction,
      HttpStatusCode expectedStatusCode,
      string? expectedContentType = "application/json") where T : class
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var results = new Dictionary<string, T>();

        foreach (var environment in GetSupportedEnvironments())
        {
            using var client = CreateClientForEnvironment(environment);

            var response = await requestAction(client);

            ValidateResponseContract(response, expectedStatusCode, expectedContentType);

            if (expectedStatusCode == HttpStatusCode.OK ||
                expectedStatusCode == HttpStatusCode.Created ||
                expectedStatusCode == HttpStatusCode.Accepted)
            {
                var contract = await ValidateJsonContract<T>(response);
                results[environment] = contract;
            }
        }

        // Validate that all environments return the same contract structure
        if (results.Count > 1)
        {
            var firstEnvironment = results.Keys.First();
            var firstContract = results[firstEnvironment];

            foreach (var (environment, contract) in results.Skip(1))
            {
                // Here we could add deep contract comparison logic
                // For now, we ensure they're both valid instances of the same type
                contract.Should().NotBeNull($"Contract in {environment} should match {firstEnvironment} environment");
                contract.GetType().Should().Be(firstContract.GetType(),
                  $"Contract type should be consistent across environments");
            }
        }

        return results;
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
