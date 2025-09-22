using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Smoke tests to validate basic multi-environment functionality
/// Ensures the API can start up and respond in different environments
/// </summary>
public class MultiEnvironmentSmokeTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public MultiEnvironmentSmokeTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the API can start up and respond to health checks
    /// </summary>
    [Fact]
    public async Task HealthCheck_ShouldRespond_Successfully()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Basic validation that health check returns expected content
        Assert.Contains("Healthy", content);
    }

    /// <summary>
    /// Validates that database operations work in testing environment
    /// </summary>
    [Fact]
    public async Task DatabaseOperations_ShouldRequireAuthentication()
    {
        // Arrange
        _factory.EnsureDatabaseCreated();
        await _factory.ClearDatabaseAsync();

        using var client = _factory.CreateClient();

        // Act - Try to get people (should return 401 because no auth)
        var response = await client.GetAsync("/api/people");

        // Assert - Should get Unauthorized, which means the endpoint exists and security is working
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Validates that authentication endpoints are available
    /// </summary>
    [Fact]
    public async Task AuthenticationEndpoints_ShouldBeAvailable()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act - Try authentication endpoints (they should exist, even if they return errors)
        var loginResponse = await client.PostAsync("/api/auth/login", new StringContent("{}"));

        // Assert - Endpoints should exist (not 404), even if they return BadRequest for empty data
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, loginResponse.StatusCode);
    }

    /// <summary>
    /// Validates that API endpoints return proper responses
    /// </summary>
    [Fact]
    public async Task ApiEndpoints_ShouldReturnProperHttpResponses()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/people");

        // Assert - Should get 401 (Unauthorized) which means endpoint exists and auth is working
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

        // Content type should be JSON (or problem+json for errors)
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.True(contentType == "application/json" || contentType == "application/problem+json");
    }
}
