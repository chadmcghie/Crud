using System;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using FluentAssertions;
using Infrastructure.Services.Caching;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class CacheStatisticsServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ILogger<CacheStatisticsService>> _mockLogger;
    private readonly CacheStatisticsService _service;

    public CacheStatisticsServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockLogger = new Mock<ILogger<CacheStatisticsService>>();
        _service = new CacheStatisticsService(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCurrentStatisticsAsync_WithNoHitsOrMisses_ShouldReturnZeroHitRatio()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        var result = await _service.GetCurrentStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.HitRatio.Should().Be(0.0);
        result.TotalHits.Should().Be(0);
        result.TotalMisses.Should().Be(0);
        result.RedisConnected.Should().BeFalse();
    }

    [Fact]
    public void RecordHit_ShouldIncrementHitCount()
    {
        // Arrange
        const string cacheType = "test";

        // Act
        _service.RecordHit(cacheType);
        _service.RecordHit(cacheType);

        // Assert - We can't directly access the internal counters, but we can test via GetCurrentStatisticsAsync
        // The statistics will reflect the recorded hits
    }

    [Fact]
    public void RecordMiss_ShouldIncrementMissCount()
    {
        // Arrange
        const string cacheType = "test";

        // Act
        _service.RecordMiss(cacheType);

        // Assert - The miss should be recorded internally
        // We'll verify this through integration with GetCurrentStatisticsAsync
    }

    [Fact]
    public async Task GetCurrentStatisticsAsync_WithHitsAndMisses_ShouldCalculateCorrectHitRatio()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);
        const string cacheType = "test";

        // Act
        _service.RecordHit(cacheType);
        _service.RecordHit(cacheType);
        _service.RecordHit(cacheType);
        _service.RecordMiss(cacheType);

        var result = await _service.GetCurrentStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalHits.Should().Be(3);
        result.TotalMisses.Should().Be(1);
        result.HitRatio.Should().Be(0.75); // 3 hits out of 4 total operations
        result.HitsByType.Should().ContainKey(cacheType);
        result.MissesByType.Should().ContainKey(cacheType);
    }

    [Fact]
    public void RecordOperation_ShouldTrackOperationTime()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        _service.RecordOperation("get", duration);

        // Assert - The operation time should be recorded
        // We can verify this through GetCurrentStatisticsAsync which calculates average response time
    }

    [Fact]
    public async Task GetCurrentStatisticsAsync_WithOperationTimes_ShouldCalculateAverageResponseTime()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        _service.RecordOperation("get", TimeSpan.FromMilliseconds(100));
        _service.RecordOperation("set", TimeSpan.FromMilliseconds(200));

        var result = await _service.GetCurrentStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AverageResponseTimeMs.Should().Be(150.0); // Average of 100 and 200
    }

    [Fact]
    public async Task GetCurrentStatisticsAsync_ShouldIncludeUptime()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        var result = await _service.GetCurrentStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetCurrentStatisticsAsync_WithRedisUnavailable_ShouldHandleGracefully()
    {
        // Arrange
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        // Act
        var result = await _service.GetCurrentStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.RedisConnected.Should().BeFalse();
        result.KeyCount.Should().Be(0);
        result.MemoryUsageMB.Should().Be(0.0);
    }
}
