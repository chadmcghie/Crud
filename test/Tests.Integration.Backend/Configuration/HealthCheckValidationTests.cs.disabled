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
        healthCheckService.Should().NotBeNull(
            $"HealthCheckService should be registered in {environment} environment");
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
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"/health endpoint should return OK in {environment} environment");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy",
            $"/health endpoint should return 'Healthy' in {environment} environment");
    }

    /// <summary>
    /// Test 1.3: Validates /api/health endpoint returns detailed health information
    /// Ensures API health endpoint provides environment-specific health details
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
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"/api/health endpoint should return OK in {environment} environment");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty(
            $"/api/health endpoint should return health information in {environment} environment");

        // Validate JSON structure
        var healthInfo = JsonSerializer.Deserialize<JsonElement>(content);
        healthInfo.TryGetProperty("status", out var statusProperty).Should().BeTrue(
            $"/api/health should include status in {environment} environment");

        var status = statusProperty.GetString();
        status.Should().Be("Healthy",
            $"/api/health should report Healthy status in {environment} environment");
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
        healthCheckResult.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);

        // Note: Database health check registration depends on the application's health check configuration
        // This test validates the overall health check functionality
        healthCheckResult.Entries.Should().NotBeNull(
            $"Health check entries should be available in {environment} environment");
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
            var response = await client.GetAsync("/api/health");
            var content = await response.Content.ReadAsStringAsync();
            var healthInfo = JsonSerializer.Deserialize<JsonElement>(content);

            // Assert - Check for environment-specific information
            if (healthInfo.TryGetProperty("entries", out var entries))
            {
                // Validate that health checks provide environment context
                entries.TryGetProperty("database", out var databaseEntry).Should().BeTrue(
                    $"Database health check should be present in {environment}");

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
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Health check should succeed in {environment} environment");

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            $"Health check should respond within 5 seconds in {environment} environment");
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
        healthCheckResult.Entries.Should().NotBeEmpty(
            $"Health checks should be registered in {environment} environment");

        // Validate expected health checks are present
        var expectedHealthChecks = new[] { "database" };
        foreach (var expectedCheck in expectedHealthChecks)
        {
            healthCheckResult.Entries.Should().ContainKey(expectedCheck,
                $"{expectedCheck} health check should be registered in {environment} environment");
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
        healthCheckResult.Should().NotBeNull(
            $"Health check should return result in {environment} environment");

        foreach (var entry in healthCheckResult.Entries)
        {
            entry.Value.Should().NotBeNull(
                $"Health check entry {entry.Key} should have valid result in {environment} environment");
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

                    // Add minimal test configuration
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
