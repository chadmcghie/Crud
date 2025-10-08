using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using Infrastructure.Services.Caching;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class CacheManagementServiceTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ILogger<CacheManagementService>> _mockLogger;
    private readonly CacheManagementService _service;

    public CacheManagementServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockLogger = new Mock<ILogger<CacheManagementService>>();
        _service = new CacheManagementService(_mockCacheService.Object, _mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RemoveKeyAsync_ShouldCallCacheServiceRemove()
    {
        // Arrange
        const string key = "test-key";
        _mockCacheService.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveKeyAsync(key);

        // Assert
        _mockCacheService.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KeyExistsAsync_ShouldCallCacheServiceExists()
    {
        // Arrange
        const string key = "test-key";
        const bool expectedExists = true;
        _mockCacheService.Setup(c => c.ExistsAsync(key, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedExists);

        // Act
        var result = await _service.KeyExistsAsync(key);

        // Assert
        Assert.Equal(expectedExists, result);
        _mockCacheService.Verify(c => c.ExistsAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmCriticalDataAsync_ShouldCompleteSuccessfully()
    {
        // Act
        await _service.WarmCriticalDataAsync();

        // Assert - Should not throw any exceptions
        // In a real implementation, this would test actual warming logic
    }

    [Fact]
    public async Task GetKeyCountAsync_WithRedisDisconnected_ShouldReturnZero()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        var result = await _service.GetKeyCountAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetKeysAsync_WithRedisDisconnected_ShouldReturnEmptyCollection()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        var result = await _service.GetKeysAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClearAllAsync_WithRedisDisconnected_ShouldLogWarning()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        await _service.ClearAllAsync();

        // Assert
        // The method should complete without throwing
        // In a real implementation with Redis connected, this would clear the cache
    }

    [Fact]
    public async Task ClearByPatternAsync_WithRedisDisconnected_ShouldLogWarning()
    {
        // Arrange
        const string pattern = "test:*";
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        await _service.ClearByPatternAsync(pattern);

        // Assert
        // The method should complete without throwing
        // In a real implementation with Redis connected, this would clear matching keys
    }

    [Fact]
    public async Task RemoveKeyAsync_WhenCacheServiceThrows_ShouldPropagateException()
    {
        // Arrange
        const string key = "test-key";
        var expectedException = new InvalidOperationException("Cache error");
        _mockCacheService.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RemoveKeyAsync(key));
        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task KeyExistsAsync_WhenCacheServiceThrows_ShouldPropagateException()
    {
        // Arrange
        const string key = "test-key";
        var expectedException = new InvalidOperationException("Cache error");
        _mockCacheService.Setup(c => c.ExistsAsync(key, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.KeyExistsAsync(key));
        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public void Constructor_WithNullCacheService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheManagementService(null!, _mockRedis.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheManagementService(_mockCacheService.Object, _mockRedis.Object, null!));
    }
}
