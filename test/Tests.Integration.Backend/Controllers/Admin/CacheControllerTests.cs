using System.Net;
using System.Text.Json;
using Api.Dtos;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Controllers.Admin;

public class CacheControllerTests : IntegrationTestBase, IClassFixture<SmokeTestWebApplicationFactory>
{
    private readonly SmokeTestWebApplicationFactory _smokeFactory;

    public CacheControllerTests(TestWebApplicationFactoryFixture fixture, SmokeTestWebApplicationFactory smokeFactory) : base(fixture)
    {
        _smokeFactory = smokeFactory;
    }

    [Fact]
    public async Task GetStatistics_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/cache/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_WithAdminAuth_ShouldReturnStats()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.GetAsync("/api/admin/cache/stats");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var stats = JsonSerializer.Deserialize<CacheStatsResponse>(content, JsonOptions);

            Assert.NotNull(stats);
            Assert.True(stats!.HitRatio >= 0);
            Assert.True(stats.TotalHits >= 0);
            Assert.True(stats.TotalMisses >= 0);
            Assert.True(stats.KeyCount >= 0);
        });
    }

    [Fact]
    public async Task ClearAllCaches_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/admin/cache/clear");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClearAllCaches_WithAdminAuth_ShouldReturnSuccess()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.DeleteAsync("/api/admin/cache/clear");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheClearResponse>(content, JsonOptions);

            Assert.NotNull(result);
            Assert.True(result!.Cleared);
            Assert.False(string.IsNullOrEmpty(result.Message));
        });
    }

    [Fact]
    public async Task RemoveKey_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/admin/cache/key/test-key");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveKey_WithAdminAuth_NonexistentKey_ShouldReturnNotFound()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.DeleteAsync("/api/admin/cache/key/nonexistent-key-12345");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    [Fact]
    public async Task WarmCaches_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/admin/cache/warm", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WarmCaches_WithAdminAuth_ShouldReturnAccepted()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.PostAsync("/api/admin/cache/warm", null);

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        });
    }

    [Fact]
    public async Task GetKeys_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/cache/keys");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetKeys_WithAdminAuth_ShouldReturnKeys()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.GetAsync("/api/admin/cache/keys?pattern=*&limit=50");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheKeyListResponse>(content, JsonOptions);

            Assert.NotNull(result);
            Assert.NotNull(result!.Keys);
            Assert.True(result.TotalCount >= 0);
            Assert.Equal("*", result.Pattern);
        });
    }

    [Fact]
    public async Task ClearCachesByPattern_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange - Use smoke factory that enforces authorization
        using var client = _smokeFactory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/admin/cache/clear/test:*");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClearCachesByPattern_WithAdminAuth_ShouldReturnSuccess()
    {
        await RunWithCleanDatabaseAsync(async () =>
        {
            // Arrange
            var adminClient = await CreateAdminClientAsync();

            // Act
            var response = await adminClient.DeleteAsync("/api/admin/cache/clear/test:*");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheClearResponse>(content, JsonOptions);

            Assert.NotNull(result);
            Assert.True(result!.Cleared);
            Assert.Contains("test:*", result.Message);
        });
    }
}
