using App.Abstractions;
using App.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for the Caching feature flag
/// Validates that caching services (LazyCache, Redis, OutputCache) are conditionally registered
/// </summary>
public class CachingFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public CachingFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that the Caching feature flag can be read from configuration
    /// </summary>
    [Fact]
    public async Task CachingFeatureFlag_ShouldBeEnabled_InTestingEnvironment()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isCachingEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.Caching);

        // Assert
        Assert.True(isCachingEnabled, "Caching feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that caching services are registered when feature flag is enabled
    /// </summary>
    [Fact]
    public void CachingServices_ShouldBeRegistered_WhenFeatureFlagEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var cacheService = scope.ServiceProvider.GetService<ICacheService>();
        var cacheManagementService = scope.ServiceProvider.GetService<ICacheManagementService>();
        var cacheStatisticsService = scope.ServiceProvider.GetService<ICacheStatisticsService>();

        // Assert
        Assert.NotNull(cacheService);
        Assert.NotNull(cacheManagementService);
        Assert.NotNull(cacheStatisticsService);
    }

    /// <summary>
    /// Validates that repository interfaces are registered when feature flag is enabled
    /// Note: Implementation may use decorators or direct repositories depending on configuration
    /// </summary>
    [Fact]
    public void Repositories_ShouldBeRegistered_WhenFeatureFlagEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var personRepo = scope.ServiceProvider.GetService<IPersonRepository>();
        var roleRepo = scope.ServiceProvider.GetService<IRoleRepository>();
        var wallRepo = scope.ServiceProvider.GetService<IWallRepository>();
        var windowRepo = scope.ServiceProvider.GetService<IWindowRepository>();

        // Assert - Repositories should be available
        Assert.NotNull(personRepo);
        Assert.NotNull(roleRepo);
        Assert.NotNull(wallRepo);
        Assert.NotNull(windowRepo);
    }

    /// <summary>
    /// Validates that output cache services are registered when feature flag is enabled
    /// </summary>
    [Fact]
    public void OutputCacheServices_ShouldBeRegistered_WhenFeatureFlagEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var outputCacheInvalidationService = scope.ServiceProvider.GetService<Api.Services.IOutputCacheInvalidationService>();

        // Assert
        Assert.NotNull(outputCacheInvalidationService);
    }

    /// <summary>
    /// Validates that cache services can perform basic operations
    /// </summary>
    [Fact]
    public async Task CacheService_ShouldPerformBasicOperations_WhenEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var testKey = $"test-key-{Guid.NewGuid()}";
        var testValue = "test-value";

        try
        {
            // Act - Set
            await cacheService.SetAsync(testKey, testValue, CacheEntryOptions.FromMinutes(1));

            // Act - Get
            var retrievedValue = await cacheService.GetAsync<string>(testKey);

            // Assert
            Assert.Equal(testValue, retrievedValue);
        }
        finally
        {
            // Cleanup
            await cacheService.RemoveAsync(testKey);
        }
    }

    /// <summary>
    /// Validates that cached repositories can perform operations
    /// </summary>
    [Fact]
    public async Task CachedRepository_ShouldPerformOperations_WhenEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var personRepo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();

        // Act
        var people = await personRepo.ListAsync();

        // Assert - Should not throw, even if empty
        Assert.NotNull(people);
    }

    /// <summary>
    /// Validates that cache statistics can be retrieved
    /// </summary>
    [Fact]
    public async Task CacheStatistics_ShouldBeAccessible_WhenEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var cacheStatisticsService = scope.ServiceProvider.GetRequiredService<ICacheStatisticsService>();

        // Act
        var stats = await cacheStatisticsService.GetCurrentStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
    }

    /// <summary>
    /// Validates that cache management service is available and can perform operations
    /// </summary>
    [Fact]
    public async Task CacheManagement_ShouldBeAccessible_WhenEnabled()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var cacheManagementService = scope.ServiceProvider.GetRequiredService<ICacheManagementService>();
        var testKey = $"test-key-{Guid.NewGuid()}";

        // Act & Assert - Management service operations should not throw
        var keyExists = await cacheManagementService.KeyExistsAsync(testKey);
        Assert.False(keyExists); // Key should not exist

        // Clear all should complete without errors
        await cacheManagementService.ClearAllAsync();
    }
}
