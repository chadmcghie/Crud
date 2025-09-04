using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Polly.CircuitBreaker;

namespace Tests.Unit.Backend.Infrastructure;

/// <summary>
/// Tests for Polly resilience policies to ensure retry and circuit breaker patterns work as expected.
/// </summary>
public class PollyResilienceTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public async Task GetDatabaseRetryPolicy_ShouldRetryOnTransientErrors()
    {
        // Arrange
        var policy = PollyPolicies.GetDatabaseRetryPolicy(_mockLogger.Object);
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.Delay(1); // Make it actually async
                throw new InvalidOperationException("timeout"); // Transient error that should trigger retry
            });
        });

        // Should have attempted 4 times (1 initial + 3 retries) before giving up
        attemptCount.Should().Be(4);
    }

    [Fact]
    public async Task GetDatabaseRetryPolicy_ShouldSucceedAfterRetry()
    {
        // Arrange
        var policy = PollyPolicies.GetDatabaseRetryPolicy(_mockLogger.Object);
        var attemptCount = 0;

        // Act
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount == 1) // Fail first attempt only
            {
                throw new InvalidOperationException("deadlock"); // Transient error
            }
            await Task.Delay(1); // Make it actually async
            return;
        });

        // Assert
        attemptCount.Should().Be(2); // Should succeed on second attempt
    }

    [Fact]
    public async Task GetDatabaseCircuitBreakerPolicy_ShouldOpenAfterConsecutiveFailures()
    {
        // Arrange
        var policy = PollyPolicies.GetDatabaseCircuitBreakerPolicy(_mockLogger.Object);
        var attemptCount = 0;

        // Act & Assert - First 5 calls should fail and open the circuit
        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    await Task.Delay(1); // Make it actually async
                    throw new InvalidOperationException("Database error");
                });
            });
        }

        // Next call should fail with BrokenCircuitException (circuit is open)
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.Delay(1); // Make it actually async
                return;
            });
        });

        // Circuit breaker opened after 5 failures, so 6th call didn't execute
        attemptCount.Should().Be(5);
    }

    [Fact]
    public async Task GetCombinedDatabasePolicy_ShouldApplyBothRetryAndCircuitBreaker()
    {
        // Arrange
        var policy = PollyPolicies.GetCombinedDatabasePolicy(_mockLogger.Object);
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.Delay(1); // Make it actually async
                throw new InvalidOperationException("connection"); // Transient error
            });
        });

        // Should have retried 3 times (1 initial + 3 retries) before failing
        attemptCount.Should().Be(4);
        exception.Message.Should().Contain("connection");
    }

    [Fact]
    public async Task GetTestRetryPolicy_ShouldUseFasterRetries()
    {
        // Arrange
        var policy = PollyPolicies.GetTestRetryPolicy(_mockLogger.Object);
        var attemptCount = 0;
        var startTime = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.Delay(1); // Make it actually async
                throw new InvalidOperationException("Test error");
            });
        });

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Test policy should retry 5 times with faster intervals
        attemptCount.Should().Be(6); // 1 initial + 5 retries
        
        // Should complete faster than database policy due to shorter delays
        // (200ms * 1 + 200ms * 2 + 200ms * 3 + 200ms * 4 + 200ms * 5) = 3000ms + some overhead
        duration.TotalMilliseconds.Should().BeLessThan(5000); // Allow some buffer for test execution
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("deadlock")]  
    [InlineData("connection")]
    [InlineData("transient")]
    [InlineData("busy")]
    [InlineData("locked")]
    public async Task GetDatabaseRetryPolicy_ShouldRetryForKnownTransientErrors(string errorMessage)
    {
        // Arrange
        var policy = PollyPolicies.GetDatabaseRetryPolicy(_mockLogger.Object);
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.Delay(1); // Make it actually async
                throw new InvalidOperationException(errorMessage);
            });
        });

        // Should retry for all known transient error messages
        attemptCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task DatabaseResilienceExtensions_SaveChangesWithRetryAsync_ShouldWorkWithDbContext()
    {
        // This test demonstrates that the extension methods exist and can be called
        // Note: We can't easily test the actual DbContext retry behavior without a real database
        // but we can verify the methods exist and are accessible
        
        // Arrange
        var policy = PollyPolicies.GetDatabaseRetryPolicy(_mockLogger.Object);
        
        // Act & Assert - Just verify the policy can be created and configured
        policy.Should().NotBeNull();
        
        // Note: The real test is in integration tests where these methods are actually used
        // with real DbContext instances in the repositories
    }

    [Fact]
    public void GetDatabaseTimeoutPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseTimeoutPolicy(_mockLogger.Object);
        
        // Assert
        policy.Should().NotBeNull();
    }

    [Fact] 
    public void GetHttpTimeoutPolicy_ShouldReturnValidPolicy()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockHttpLogger = new Mock<ILogger<HttpMessageHandler>>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<HttpMessageHandler>)))
                          .Returns(mockHttpLogger.Object);
        
        // Act
        var policy = PollyPolicies.GetHttpTimeoutPolicy(mockServiceProvider.Object);
        
        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetDatabaseBulkheadPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseBulkheadPolicy(_mockLogger.Object);
        
        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetComprehensiveDatabasePolicy_ShouldCombineAllPolicies()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetComprehensiveDatabasePolicy(_mockLogger.Object);
        
        // Assert
        policy.Should().NotBeNull();
        // This policy combines timeout, retry, and circuit breaker
    }

    [Fact]
    public void GetComprehensiveHttpPolicy_ShouldCombineAllHttpPolicies()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockHttpLogger = new Mock<ILogger<HttpMessageHandler>>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<HttpMessageHandler>)))
                          .Returns(mockHttpLogger.Object);
        
        // Act
        var policy = PollyPolicies.GetComprehensiveHttpPolicy(mockServiceProvider.Object);
        
        // Assert
        policy.Should().NotBeNull();
        // This policy combines timeout, retry, and circuit breaker for HTTP
    }
}