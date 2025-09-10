using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using FluentAssertions;
using Infrastructure.Services.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class CacheServiceTests
{
    public class ICacheServiceTests
    {
        private readonly Mock<ICacheService> _mockCacheService;

        public ICacheServiceTests()
        {
            _mockCacheService = new Mock<ICacheService>();
        }

        [Fact]
        public async Task GetAsync_WithExistingKey_ShouldReturnCachedValue()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestCacheItem { Id = 1, Name = "Test" };
            _mockCacheService
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedValue);

            // Act
            var result = await _mockCacheService.Object.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedValue);
            _mockCacheService.Verify(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // Arrange
            var key = "non-existing-key";
            _mockCacheService
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TestCacheItem?)null);

            // Act
            var result = await _mockCacheService.Object.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithValidData_ShouldStoreValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };

            // Act
            await _mockCacheService.Object.SetAsync(key, value, options);

            // Assert
            _mockCacheService.Verify(
                x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_WithExistingKey_ShouldRemoveFromCache()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _mockCacheService.Object.RemoveAsync(key);

            // Assert
            _mockCacheService.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var key = "test-key";
            _mockCacheService
                .Setup(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _mockCacheService.Object.ExistsAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            var key = "non-existing-key";
            _mockCacheService
                .Setup(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _mockCacheService.Object.ExistsAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WithNonExistingKey_ShouldCallFactoryAndCache()
        {
            // Arrange
            var key = "test-key";
            var expectedValue = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1) };

            _mockCacheService
                .Setup(x => x.GetOrSetAsync(
                    key,
                    It.IsAny<Func<CancellationToken, Task<TestCacheItem>>>(),
                    options,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedValue);

            // Act
            var result = await _mockCacheService.Object.GetOrSetAsync(
                key,
                ct => Task.FromResult(expectedValue),
                options);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedValue);
        }

        [Fact]
        public async Task RemoveByPatternAsync_WithMatchingPattern_ShouldRemoveMultipleKeys()
        {
            // Arrange
            var pattern = "user:*";

            // Act
            await _mockCacheService.Object.RemoveByPatternAsync(pattern);

            // Assert
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(pattern, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetManyAsync_WithValidKeys_ShouldReturnMultipleValues()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            var expectedValues = new Dictionary<string, TestCacheItem?>
            {
                ["key1"] = new TestCacheItem { Id = 1, Name = "Item1" },
                ["key2"] = new TestCacheItem { Id = 2, Name = "Item2" },
                ["key3"] = null
            };

            _mockCacheService
                .Setup(x => x.GetManyAsync<TestCacheItem>(keys, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedValues);

            // Act
            var result = await _mockCacheService.Object.GetManyAsync<TestCacheItem>(keys);

            // Assert
            result.Should().HaveCount(3);
            result["key1"].Should().NotBeNull();
            result["key1"]!.Id.Should().Be(1);
            result["key2"].Should().NotBeNull();
            result["key3"].Should().BeNull();
        }

        [Fact]
        public async Task SetManyAsync_WithMultipleItems_ShouldCacheAll()
        {
            // Arrange
            var items = new Dictionary<string, TestCacheItem>
            {
                ["key1"] = new TestCacheItem { Id = 1, Name = "Item1" },
                ["key2"] = new TestCacheItem { Id = 2, Name = "Item2" }
            };
            var options = new CacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) };

            // Act
            await _mockCacheService.Object.SetManyAsync(items, options);

            // Assert
            _mockCacheService.Verify(
                x => x.SetManyAsync(items, options, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private class TestCacheItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }

    public class InMemoryCacheServiceTests
    {
        private readonly InMemoryCacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<InMemoryCacheService>> _mockLogger;

        public InMemoryCacheServiceTests()
        {
            _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            _mockLogger = new Mock<ILogger<InMemoryCacheService>>();
            _cacheService = new InMemoryCacheService(_memoryCache, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WithExistingKey_ShouldReturnCachedValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            _memoryCache.Set(key, value);

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(value);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // Arrange
            var key = "non-existing-key";

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithAbsoluteExpiration_ShouldStoreWithExpiry()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(1)
            };

            // Act
            await _cacheService.SetAsync(key, value, options);
            var resultBeforeExpiry = await _cacheService.GetAsync<TestCacheItem>(key);

            await Task.Delay(1500); // Wait for expiration
            var resultAfterExpiry = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            resultBeforeExpiry.Should().NotBeNull();
            resultAfterExpiry.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithSlidingExpiration_ShouldResetExpiryOnAccess()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(2)
            };

            // Act
            await _cacheService.SetAsync(key, value, options);

            // Access multiple times within sliding window
            await Task.Delay(1000);
            var result1 = await _cacheService.GetAsync<TestCacheItem>(key);

            await Task.Delay(1000);
            var result2 = await _cacheService.GetAsync<TestCacheItem>(key);

            // Should still be cached due to sliding expiration
            result2.Should().NotBeNull();

            // Wait without accessing
            await Task.Delay(2500);
            var result3 = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result3.Should().BeNull(); // Should have expired
        }

        [Fact]
        public async Task RemoveAsync_WithExistingKey_ShouldRemoveFromCache()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            _memoryCache.Set(key, value);

            // Act
            await _cacheService.RemoveAsync(key);
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var key = "test-key";
            _memoryCache.Set(key, "value");

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            var key = "non-existing-key";

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WithExistingKey_ShouldNotCallFactory()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = new TestCacheItem { Id = 1, Name = "Cached" };
            _memoryCache.Set(key, cachedValue);

            var factoryCalled = false;
            Func<CancellationToken, Task<TestCacheItem>> factory = ct =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestCacheItem { Id = 2, Name = "New" });
            };

            // Act
            var result = await _cacheService.GetOrSetAsync(key, factory, new CacheEntryOptions());

            // Assert
            result.Should().BeEquivalentTo(cachedValue);
            factoryCalled.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WithNonExistingKey_ShouldCallFactoryAndCache()
        {
            // Arrange
            var key = "test-key";
            var newValue = new TestCacheItem { Id = 2, Name = "New" };

            var factoryCalled = false;
            Func<CancellationToken, Task<TestCacheItem>> factory = ct =>
            {
                factoryCalled = true;
                return Task.FromResult(newValue);
            };

            // Act
            var result = await _cacheService.GetOrSetAsync(key, factory, new CacheEntryOptions());
            var cachedResult = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeEquivalentTo(newValue);
            cachedResult.Should().BeEquivalentTo(newValue);
            factoryCalled.Should().BeTrue();
        }

        private class TestCacheItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }

    public class RedisCacheServiceTests
    {
        private readonly Mock<IConnectionMultiplexer> _mockRedisConnection;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
        private readonly RedisCacheService _cacheService;

        public RedisCacheServiceTests()
        {
            _mockRedisConnection = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>();
            _mockLogger = new Mock<ILogger<RedisCacheService>>();

            _mockRedisConnection
                .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);

            _cacheService = new RedisCacheService(_mockRedisConnection.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WithExistingKey_ShouldReturnDeserializedValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);

            _mockDatabase
                .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue(serializedValue));

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(value.Id);
            result.Name.Should().Be(value.Name);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // Arrange
            var key = "non-existing-key";

            _mockDatabase
                .Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithAbsoluteExpiration_ShouldSerializeAndStore()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Act
            await _cacheService.SetAsync(key, value, options);

            // Assert
            _mockDatabase.Verify(
                x => x.StringSetAsync(
                    key,
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    false,
                    When.Always,
                    CommandFlags.None),
                Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldDeleteKey()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            _mockDatabase.Verify(
                x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var key = "test-key";

            _mockDatabase
                .Setup(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveByPatternAsync_ShouldDeleteMatchingKeys()
        {
            // Arrange
            var pattern = "user:*";
            var server = new Mock<IServer>();
            var matchingKeys = new[] { (RedisKey)"user:1", (RedisKey)"user:2" };

            _mockRedisConnection
                .Setup(x => x.GetEndPoints(It.IsAny<bool>()))
                .Returns(new[] { new System.Net.IPEndPoint(0, 0) });

            _mockRedisConnection
                .Setup(x => x.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>()))
                .Returns(server.Object);

            server
                .Setup(x => x.KeysAsync(
                    It.IsAny<int>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()))
                .Returns(ToAsyncEnumerable(matchingKeys));

            // Act
            await _cacheService.RemoveByPatternAsync(pattern);

            // Assert
            _mockDatabase.Verify(
                x => x.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task GetManyAsync_ShouldReturnMultipleValues()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            var value1 = System.Text.Json.JsonSerializer.Serialize(new TestCacheItem { Id = 1, Name = "Item1" });
            var value2 = System.Text.Json.JsonSerializer.Serialize(new TestCacheItem { Id = 2, Name = "Item2" });

            _mockDatabase
                .Setup(x => x.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue[] { value1, value2, RedisValue.Null });

            // Act
            var result = await _cacheService.GetManyAsync<TestCacheItem>(keys);

            // Assert
            result.Should().HaveCount(3);
            result["key1"].Should().NotBeNull();
            result["key1"]!.Id.Should().Be(1);
            result["key2"].Should().NotBeNull();
            result["key3"].Should().BeNull();
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }

        private class TestCacheItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }

    public class CompositeCacheServiceTests
    {
        private readonly Mock<ICacheService> _mockPrimaryCache;
        private readonly Mock<ICacheService> _mockFallbackCache;
        private readonly Mock<ILogger<CompositeCacheService>> _mockLogger;
        private readonly CompositeCacheService _cacheService;

        public CompositeCacheServiceTests()
        {
            _mockPrimaryCache = new Mock<ICacheService>();
            _mockFallbackCache = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<CompositeCacheService>>();

            _cacheService = new CompositeCacheService(
                _mockPrimaryCache.Object,
                _mockFallbackCache.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WhenPrimarySucceeds_ShouldNotUseFallback()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };

            _mockPrimaryCache
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeEquivalentTo(value);
            _mockPrimaryCache.Verify(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()), Times.Once);
            _mockFallbackCache.Verify(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_WhenPrimaryFails_ShouldUseFallback()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };

            _mockPrimaryCache
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

            _mockFallbackCache
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value);

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeEquivalentTo(value);
            _mockFallbackCache.Verify(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetAsync_ShouldSetInBothCaches()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions();

            // Act
            await _cacheService.SetAsync(key, value, options);

            // Assert
            _mockPrimaryCache.Verify(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()), Times.Once);
            _mockFallbackCache.Verify(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WhenPrimaryFails_ShouldStillSetInFallback()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions();

            _mockPrimaryCache
                .Setup(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

            // Act
            await _cacheService.SetAsync(key, value, options);

            // Assert
            _mockFallbackCache.Verify(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveFromBothCaches()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            _mockPrimaryCache.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
            _mockFallbackCache.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenNotInEitherCache_ShouldCallFactoryAndSetBoth()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions();

            _mockPrimaryCache
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TestCacheItem?)null);

            _mockFallbackCache
                .Setup(x => x.GetAsync<TestCacheItem>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TestCacheItem?)null);

            Func<CancellationToken, Task<TestCacheItem>> factory = ct => Task.FromResult(value);

            // Act
            var result = await _cacheService.GetOrSetAsync(key, factory, options);

            // Assert
            result.Should().BeEquivalentTo(value);
            _mockPrimaryCache.Verify(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()), Times.Once);
            _mockFallbackCache.Verify(x => x.SetAsync(key, value, options, It.IsAny<CancellationToken>()), Times.Once);
        }

        private class TestCacheItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }

    public class LazyCacheServiceTests
    {
        private readonly LazyCacheService _cacheService;
        private readonly Mock<ILogger<LazyCacheService>> _mockLogger;

        public LazyCacheServiceTests()
        {
            _mockLogger = new Mock<ILogger<LazyCacheService>>();
            var lazyCacheOptions = Options.Create(new LazyCacheOptions
            {
                DefaultCachePolicy = new CacheDefaults
                {
                    DefaultCacheDurationSeconds = 60
                }
            });
            _cacheService = new LazyCacheService(lazyCacheOptions, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WithExistingKey_ShouldReturnCachedValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            await _cacheService.SetAsync(key, value, new CacheEntryOptions());

            // Act
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(value);
        }

        [Fact]
        public async Task GetOrSetAsync_WithConcurrentRequests_ShouldOnlyCallFactoryOnce()
        {
            // Arrange
            var key = "test-key";
            var factoryCallCount = 0;

            Func<CancellationToken, Task<TestCacheItem>> factory = async ct =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(100); // Simulate slow operation
                return new TestCacheItem { Id = 1, Name = "Test" };
            };

            // Act - Multiple concurrent requests
            var tasks = new List<Task<TestCacheItem?>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_cacheService.GetOrSetAsync(key, factory, new CacheEntryOptions()));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            factoryCallCount.Should().Be(1); // Factory should only be called once
            results.Should().AllBeEquivalentTo(results[0]); // All results should be the same
        }

        [Fact]
        public async Task SetAsync_WithPriority_ShouldRespectCachePriority()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            var options = new CacheEntryOptions
            {
                Priority = global::App.Interfaces.CacheItemPriority.High
            };

            // Act
            await _cacheService.SetAsync(key, value, options);
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(value);
        }

        [Fact]
        public async Task RemoveAsync_WithExistingKey_ShouldRemoveFromCache()
        {
            // Arrange
            var key = "test-key";
            var value = new TestCacheItem { Id = 1, Name = "Test" };
            await _cacheService.SetAsync(key, value, new CacheEntryOptions());

            // Act
            await _cacheService.RemoveAsync(key);
            var result = await _cacheService.GetAsync<TestCacheItem>(key);

            // Assert
            result.Should().BeNull();
        }

        private class TestCacheItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
