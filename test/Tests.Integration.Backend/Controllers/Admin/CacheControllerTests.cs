using System.Net;
using System.Text.Json;
using Api.Dtos;
using FluentAssertions;
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var stats = JsonSerializer.Deserialize<CacheStatsResponse>(content, JsonOptions);

            stats.Should().NotBeNull();
            stats.HitRatio.Should().BeGreaterThanOrEqualTo(0);
            stats.TotalHits.Should().BeGreaterThanOrEqualTo(0);
            stats.TotalMisses.Should().BeGreaterThanOrEqualTo(0);
            stats.KeyCount.Should().BeGreaterThanOrEqualTo(0);
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheClearResponse>(content, JsonOptions);

            result.Should().NotBeNull();
            result.Cleared.Should().BeTrue();
            result.Message.Should().NotBeNullOrEmpty();
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheKeyListResponse>(content, JsonOptions);

            result.Should().NotBeNull();
            result.Keys.Should().NotBeNull();
            result.TotalCount.Should().BeGreaterThanOrEqualTo(0);
            result.Pattern.Should().Be("*");
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CacheClearResponse>(content, JsonOptions);

            result.Should().NotBeNull();
            result.Cleared.Should().BeTrue();
            result.Message.Should().Contain("test:*");
        });
    }
}
