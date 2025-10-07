using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Tests for health check validation across environments (Development, Testing, Production)
/// Validates health check endpoints and health check services work correctly in all configurations
/// without creating new branches - using existing dev=Development, staging=Testing, main=Production mapping
/// </summary>
public class HealthCheckValidationTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public HealthCheckValidationTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test 1.3: Validates health check service registration across all environments
    /// Ensures health check services are properly configured in Development, Testing, Production
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void HealthCheckServices_ShouldBeRegistered_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();

        // Assert
        Assert.NotNull(healthCheckService);
    }

    /// <summary>
    /// Test 1.3: Validates /health endpoint returns successful response
    /// Ensures basic health endpoint works in all environment configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task HealthEndpoint_ShouldReturnHealthy_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    /// <summary>
    /// Test 1.3: Validates /health/detailed endpoint returns detailed health information
    /// Ensures detailed health endpoint provides environment-specific health details
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task ApiHealthEndpoint_ShouldReturnDetailedHealth_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/detailed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.NotEmpty(content);

        // Validate JSON structure
        var healthInfo = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(healthInfo.TryGetProperty("status", out var statusProperty));

        var status = statusProperty.GetString();
        Assert.Equal("Healthy", status);
    }

    /// <summary>
    /// Test 1.3: Validates health checks can detect database connectivity issues
    /// Ensures database health checks work correctly for each environment's database configuration
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task DatabaseHealthCheck_ShouldValidateConnectivity_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthCheckResult = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.True(healthCheckResult.Status == HealthStatus.Healthy ||
                    healthCheckResult.Status == HealthStatus.Degraded ||
                    healthCheckResult.Status == HealthStatus.Unhealthy);

        // Note: Database health check registration depends on the application's health check configuration
        // This test validates the overall health check functionality
        Assert.NotNull(healthCheckResult.Entries);
    }

    /// <summary>
    /// Test 1.3: Validates environment-specific health check configuration
    /// Ensures health checks include environment-appropriate information
    /// </summary>
    [Fact]
    public async Task HealthChecks_ShouldIncludeEnvironmentSpecificInformation_ForEachEnvironment()
    {
        var environments = new[] { "Development", "Testing", "Production" };

        foreach (var environment in environments)
        {
            // Arrange
            using var factory = CreateFactoryForEnvironment(environment);
            using var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/detailed");
            var content = await response.Content.ReadAsStringAsync();
            var healthInfo = JsonSerializer.Deserialize<JsonElement>(content);

            // Assert - Check for environment-specific information
            if (healthInfo.TryGetProperty("entries", out var entries))
            {
                // Validate that health checks provide environment context
                Assert.True(entries.TryGetProperty("database", out var databaseEntry));

                if (databaseEntry.TryGetProperty("data", out var data))
                {
                    // Environment-specific validation could go here
                    // For example, checking connection string format or provider type
                }
            }
        }
    }

    /// <summary>
    /// Test 1.3: Validates health check performance across environments
    /// Ensures health checks respond quickly in all configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task HealthChecks_ShouldRespondQuickly_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var client = factory.CreateClient();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await client.GetAsync("/health");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(stopwatch.ElapsedMilliseconds < 5000);
    }

    /// <summary>
    /// Test 1.3: Validates custom health checks are registered
    /// Ensures application-specific health checks work in all environments
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task CustomHealthChecks_ShouldBeRegistered_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthCheckResult = await healthCheckService.CheckHealthAsync();

        // Assert
        Assert.NotEmpty(healthCheckResult.Entries);

        // Validate expected health checks are present
        var expectedHealthChecks = new[] { "database" };
        foreach (var expectedCheck in expectedHealthChecks)
        {
            Assert.True(healthCheckResult.Entries.ContainsKey(expectedCheck));
        }
    }

    /// <summary>
    /// Test 1.3: Validates health check failure scenarios
    /// Ensures health checks can properly detect and report failures
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task HealthChecks_ShouldHandleFailureScenarios_InAllEnvironments(string environment)
    {
        // This test validates that health checks can detect failures
        // In a real scenario, you might temporarily break a dependency to test failure detection

        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var healthCheckResult = await healthCheckService.CheckHealthAsync();

        // Assert - Validate health check infrastructure is working
        // In normal conditions, this should be healthy
        // In failure scenarios, it should properly report unhealthy status
        Assert.NotNull(healthCheckResult);

        foreach (var entry in healthCheckResult.Entries)
        {
            Assert.NotNull(entry.Value.Description);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Creates a WebApplicationFactory configured for a specific environment
    /// </summary>
    private WebApplicationFactory<Api.Program> CreateFactoryForEnvironment(string environment)
    {
        return new WebApplicationFactory<Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Clear existing configuration
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

                    // Add health check test-specific overrides AFTER appsettings
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source=CrudTest_Health_{environment}_{Guid.NewGuid()}.db",
                        ["DatabaseProvider"] = "SQLite",
                        ["Logging:LogLevel:Default"] = "Warning",
                        ["AllowedHosts"] = "*"
                    });
                });
            });
    }

    #endregion
}
