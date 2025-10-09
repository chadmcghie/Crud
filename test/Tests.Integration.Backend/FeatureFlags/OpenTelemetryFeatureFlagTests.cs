using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the OpenTelemetry feature flag
/// Validates that OpenTelemetry tracing and metrics are conditionally enabled
/// </summary>
public class OpenTelemetryFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public OpenTelemetryFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the OpenTelemetry feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task OpenTelemetryFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isOpenTelemetryEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.OpenTelemetry);

        // Assert
        Assert.True(isOpenTelemetryEnabled, "OpenTelemetry feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that OpenTelemetry tracing services are registered when feature flag is on
    /// </summary>
    [Fact]
    public void OpenTelemetryTracing_ShouldBeRegistered_WhenFeatureFlagEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var tracerProvider = scope.ServiceProvider.GetService<TracerProvider>();

        // Assert
        Assert.NotNull(tracerProvider);
    }

    /// <summary>
    /// Validates that OpenTelemetry metrics services are registered when feature flag is on
    /// </summary>
    [Fact]
    public void OpenTelemetryMetrics_ShouldBeRegistered_WhenFeatureFlagEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var meterProvider = scope.ServiceProvider.GetService<MeterProvider>();

        // Assert
        Assert.NotNull(meterProvider);
    }

    /// <summary>
    /// Validates that API returns valid responses when OpenTelemetry is enabled
    /// </summary>
    [Fact]
    public async Task ApiResponses_ShouldBeValid_WithOpenTelemetryEnabled()
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
    /// Validates that OpenTelemetry doesn't interfere with normal request processing
    /// </summary>
    [Fact]
    public async Task OpenTelemetry_ShouldNotInterruptRequestProcessing()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make multiple requests to generate traces
        var response1 = await client.GetAsync("/health");
        var response2 = await client.GetAsync("/health/detailed");
        var response3 = await client.GetAsync("/health");

        // Assert - All requests should succeed
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        response3.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Validates that OpenTelemetry is configured for the application
    /// </summary>
    [Fact]
    public async Task OpenTelemetry_ShouldBeConfigured_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make requests to different endpoints to generate telemetry
        var healthResponse = await client.GetAsync("/health");
        var detailedHealthResponse = await client.GetAsync("/health/detailed");

        // Assert - All endpoints should work with OpenTelemetry enabled
        healthResponse.EnsureSuccessStatusCode();
        detailedHealthResponse.EnsureSuccessStatusCode();

        // Additional validation: Telemetry should be initialized
        using var scope = _factory.Services.CreateScope();
        var tracerProvider = scope.ServiceProvider.GetService<TracerProvider>();
        var meterProvider = scope.ServiceProvider.GetService<MeterProvider>();

        Assert.NotNull(tracerProvider);
        Assert.NotNull(meterProvider);
    }
}
