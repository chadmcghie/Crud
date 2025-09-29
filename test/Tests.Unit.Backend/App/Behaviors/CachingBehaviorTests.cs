using System;
using System.Threading;
using System.Threading.Tasks;
using App.Behaviors;
using App.Interfaces;
using App.Services;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.App.Behaviors;

public class CachingBehaviorTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ILogger<CachingBehavior<TestQuery, TestResponse>>> _loggerMock;
    private readonly CachingBehavior<TestQuery, TestResponse> _behavior;

    public CachingBehaviorTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _loggerMock = new Mock<ILogger<CachingBehavior<TestQuery, TestResponse>>>();
        _behavior = new CachingBehavior<TestQuery, TestResponse>(
            _cacheServiceMock.Object,
            _keyGeneratorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_ShouldCallNext()
    {
        // Arrange
        var request = new NonCacheableQuery();
        var response = new TestResponse { Data = "test" };
        var called = false;

        // Create behavior for non-cacheable query
        var loggerMock = new Mock<ILogger<CachingBehavior<NonCacheableQuery, TestResponse>>>();
        var behavior = new CachingBehavior<NonCacheableQuery, TestResponse>(
            _cacheServiceMock.Object,
            _keyGeneratorMock.Object,
            loggerMock.Object);

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            called = true;
            return Task.FromResult(response);
        };
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        called.Should().BeTrue();
        _cacheServiceMock.Verify(x => x.GetAsync<TestResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        var cachedResponse = new TestResponse { Data = "cached" };
        var cacheKey = "TestQuery:1";

        _keyGeneratorMock.Setup(x => x.GenerateKey<TestQuery>(It.IsAny<TestQuery>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var called = false;

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            called = true;
            return Task.FromResult(new TestResponse { Data = "new" });
        };
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResponse);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCallNextAndCache()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "TestQuery:1";

        _keyGeneratorMock.Setup(x => x.GenerateKey<TestQuery>(It.IsAny<TestQuery>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse?)null);

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(response);
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _cacheServiceMock.Verify(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            response,
            It.IsAny<CacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheableWithCustomTTL_ShouldUseCustomTTL()
    {
        // Arrange
        var request = new CustomTTLQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "CustomTTLQuery:1";
        var expectedTTL = TimeSpan.FromSeconds(1800);

        var keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        var loggerMock = new Mock<ILogger<CachingBehavior<CustomTTLQuery, TestResponse>>>();

        keyGeneratorMock.Setup(x => x.GenerateKey<CustomTTLQuery>(It.IsAny<CustomTTLQuery>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(cacheKey);

        var customBehavior = new CachingBehavior<CustomTTLQuery, TestResponse>(
            _cacheServiceMock.Object,
            keyGeneratorMock.Object,
            loggerMock.Object);

        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse?)null);

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(response);
        var result = await customBehavior.Handle(request, next, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey,
            response,
            It.Is<CacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expectedTTL),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheGetThrows_ShouldContinueAndCallNext()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "TestQuery:1";

        _keyGeneratorMock.Setup(x => x.GenerateKey<TestQuery>(It.IsAny<TestQuery>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(response);
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResponseIsNull_ShouldNotCache()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        TestResponse? response = null;
        var cacheKey = "TestQuery:1";

        _keyGeneratorMock.Setup(x => x.GenerateKey<TestQuery>(It.IsAny<TestQuery>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse?)null);

        // Act
        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(response!);
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<TestResponse>(),
            It.IsAny<CacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Test classes
    [CacheableAttribute(durationInSeconds: 300)]
    public class TestQuery : IRequest<TestResponse>
    {
        public int Id { get; set; }
    }

    public class NonCacheableQuery : IRequest<TestResponse>
    {
        public int Id { get; set; }
    }

    [CacheableAttribute(durationInSeconds: 1800)]
    public class CustomTTLQuery : IRequest<TestResponse>
    {
        public int Id { get; set; }
    }

    public class TestResponse
    {
        public string Data { get; set; } = string.Empty;
    }
}
