using App.Behaviors;
using App.Common.Attributes;
using App.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
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
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            called = true;
            return Task.FromResult(response);
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

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
        
        _keyGeneratorMock.Setup(x => x.GenerateKey(request))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var called = false;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            called = true;
            return Task.FromResult(new TestResponse { Data = "new" });
        };

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResponse);
        called.Should().BeFalse();
        _cacheServiceMock.Verify(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCallNextAndCacheResult()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "TestQuery:1";
        
        _keyGeneratorMock.Setup(x => x.GenerateKey(request))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse)null);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _cacheServiceMock.Verify(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey, 
            response, 
            It.IsAny<TimeSpan>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheableWithCustomTTL_ShouldUseCustomTTL()
    {
        // Arrange
        var request = new CustomTTLQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "CustomTTLQuery:1";
        var expectedTTL = TimeSpan.FromMinutes(30);
        
        _keyGeneratorMock.Setup(x => x.GenerateKey(request))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse)null);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(x => x.SetAsync(
            cacheKey, 
            response, 
            It.Is<TimeSpan>(t => t == expectedTTL), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheThrowsException_ShouldCallNextAndLogError()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        var response = new TestResponse { Data = "new" };
        var cacheKey = "TestQuery:1";
        
        _keyGeneratorMock.Setup(x => x.GenerateKey(request))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Cache error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResponseIsNull_ShouldNotCache()
    {
        // Arrange
        var request = new TestQuery { Id = 1 };
        TestResponse response = null;
        var cacheKey = "TestQuery:1";
        
        _keyGeneratorMock.Setup(x => x.GenerateKey(request))
            .Returns(cacheKey);
        _cacheServiceMock.Setup(x => x.GetAsync<TestResponse>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestResponse)null);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(response);

        // Act
        var result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _cacheServiceMock.Verify(x => x.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<TestResponse>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Test classes
    [Cacheable(DurationInSeconds = 300)]
    private class TestQuery : IRequest<TestResponse>
    {
        public int Id { get; set; }
    }

    private class NonCacheableQuery : TestQuery
    {
    }

    [Cacheable(DurationInSeconds = 1800)]
    private class CustomTTLQuery : IRequest<TestResponse>
    {
        public int Id { get; set; }
    }

    private class TestResponse
    {
        public string Data { get; set; }
    }
}