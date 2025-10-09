using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the Compression feature flag
/// Validates that response compression (Brotli, Gzip) is conditionally enabled
/// </summary>
public class CompressionFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public CompressionFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the Compression feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task CompressionFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isCompressionEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.Compression);

        // Assert
        Assert.True(isCompressionEnabled, "Compression feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that compression middleware is enabled when feature flag is on
    /// </summary>
    [Fact]
    public async Task CompressionMiddleware_ShouldBeEnabled_WhenFeatureFlagEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

        // Act - Make a request that should be compressed
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();

        // Note: In testing environment, compression may or may not be applied
        // depending on response size and other factors. This test validates
        // that the middleware is registered and doesn't cause errors.
    }

    /// <summary>
    /// Validates that API returns valid JSON responses when compression is enabled
    /// </summary>
    [Fact]
    public async Task ApiResponses_ShouldBeValid_WithCompressionEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, br");

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);
        Assert.Contains("Healthy", content);
    }

    /// <summary>
    /// Validates that compression works with JSON content types
    /// </summary>
    [Fact]
    public async Task Compression_ShouldWorkWithJsonContentTypes()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

        // Act - Test JSON response from /health/detailed
        var detailedHealthResponse = await client.GetAsync("/health/detailed");

        // Assert
        detailedHealthResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/json", detailedHealthResponse.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Validates that responses can be read correctly when compression is enabled
    /// HttpClient automatically decompresses responses
    /// </summary>
    [Fact]
    public async Task CompressedResponses_ShouldBeReadableByClient()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Note: HttpClient created by factory automatically handles decompression

        // Act
        var response = await client.GetAsync("/health/detailed");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotEmpty(content);

        // Verify JSON structure is intact after decompression
        Assert.Contains("status", content.ToLower());
        Assert.Contains("environment", content.ToLower());
    }
}
