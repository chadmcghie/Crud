using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the HealthChecksDetailed feature flag
/// Validates that detailed health check endpoint is conditionally enabled
/// </summary>
public class HealthChecksDetailedFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public HealthChecksDetailedFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the HealthChecksDetailed feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task HealthChecksDetailedFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isHealthChecksDetailedEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.HealthChecksDetailed);

        // Assert
        Assert.True(isHealthChecksDetailedEnabled, "HealthChecksDetailed feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that detailed health check endpoint returns comprehensive information when enabled
    /// </summary>
    [Fact]
    public async Task DetailedHealthCheckEndpoint_ShouldReturnComprehensiveInfo_WhenEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/detailed");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);

        // Detailed health check should include environment, database provider, etc.
        Assert.Contains("status", content.ToLower());
        Assert.Contains("environment", content.ToLower());
        Assert.Contains("databaseprovider", content.ToLower());
    }

    /// <summary>
    /// Validates that basic health check always works
    /// </summary>
    [Fact]
    public async Task BasicHealthCheck_ShouldAlwaysWork()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);
        Assert.Contains("Healthy", content);
    }

    /// <summary>
    /// Validates that readiness probe works
    /// </summary>
    [Fact]
    public async Task ReadinessProbe_ShouldWork()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);
    }

    /// <summary>
    /// Validates that health check services are registered
    /// </summary>
    [Fact]
    public void HealthCheckServices_ShouldBeRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var healthCheckService = scope.ServiceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        // Assert
        Assert.NotNull(healthCheckService);
    }
}
