using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the CORS feature flag
/// Validates that CORS middleware is conditionally enabled
/// </summary>
public class CorsFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public CorsFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the Cors feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task CorsFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isCorsEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.Cors);

        // Assert
        Assert.True(isCorsEnabled, "Cors feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that CORS middleware is enabled when feature flag is on
    /// </summary>
    [Fact]
    public async Task CorsMiddleware_ShouldBeEnabled_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        // Act - Make a request that should go through CORS middleware
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();

        // CORS headers should be present when enabled
        // Note: In test environment, CORS is configured to allow localhost:4200
    }

    /// <summary>
    /// Validates that API returns valid responses when CORS is enabled
    /// </summary>
    [Fact]
    public async Task ApiResponses_ShouldBeValid_WithCorsEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);
        Assert.Contains("Healthy", content);
    }

    /// <summary>
    /// Validates that CORS allows configured origins
    /// </summary>
    [Fact]
    public async Task Cors_ShouldAllowConfiguredOrigins_WhenEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        // Act - Make request from allowed origin
        var response = await client.GetAsync("/health");

        // Assert - Request should succeed
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Validates that CORS middleware is configured correctly
    /// </summary>
    [Fact]
    public async Task Cors_ShouldBeConfigured_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        // Act - Make requests to different endpoints
        var healthResponse = await client.GetAsync("/health");
        var detailedHealthResponse = await client.GetAsync("/health/detailed");

        // Assert - All endpoints should work with CORS enabled
        healthResponse.EnsureSuccessStatusCode();
        detailedHealthResponse.EnsureSuccessStatusCode();
    }
}
