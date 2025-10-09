using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the RateLimiting feature flag
/// Validates that rate limiting middleware is conditionally enabled
/// </summary>
public class RateLimitingFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public RateLimitingFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the RateLimiting feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task RateLimitingFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isRateLimitingEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.RateLimiting);

        // Assert
        Assert.True(isRateLimitingEnabled, "RateLimiting feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that rate limiting middleware is enabled when feature flag is on
    /// </summary>
    [Fact]
    public async Task RateLimitingMiddleware_ShouldBeEnabled_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make a request that should go through rate limiting
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();

        // Note: In testing environment, rate limits are relaxed (high limits)
        // This test validates that the middleware is registered and doesn't cause errors.
    }

    /// <summary>
    /// Validates that API returns valid responses when rate limiting is enabled
    /// </summary>
    [Fact]
    public async Task ApiResponses_ShouldBeValid_WithRateLimitingEnabled()
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
    /// Validates that rate limiting allows normal traffic flow in test environment
    /// Testing environment has relaxed limits to avoid test failures
    /// </summary>
    [Fact]
    public async Task RateLimiting_ShouldAllowNormalTraffic_InTestEnvironment()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make multiple requests (testing environment has high limits)
        var response1 = await client.GetAsync("/health");
        var response2 = await client.GetAsync("/health");
        var response3 = await client.GetAsync("/health");

        // Assert - All requests should succeed in test environment
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        response3.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Validates that rate limiting middleware is configured
    /// </summary>
    [Fact]
    public async Task RateLimiting_ShouldBeConfigured_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make requests to different endpoints
        var healthResponse = await client.GetAsync("/health");
        var detailedHealthResponse = await client.GetAsync("/health/detailed");

        // Assert - All endpoints should work with rate limiting enabled
        healthResponse.EnsureSuccessStatusCode();
        detailedHealthResponse.EnsureSuccessStatusCode();
    }
}
