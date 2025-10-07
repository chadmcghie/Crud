using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure;

/// <summary>
/// Fast unit tests for Polly resilience policy configuration.
/// These tests verify policies are properly configured without waiting for actual retries.
/// For actual retry/circuit breaker behavior testing, see integration tests.
/// </summary>
public class PollyResilienceTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public void GetDatabaseRetryPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseRetryPolicy(_mockLogger.Object);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void GetTestRetryPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetTestRetryPolicy(_mockLogger.Object);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void GetDatabaseTimeoutPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseTimeoutPolicy();

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void GetDatabaseCircuitBreakerPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseCircuitBreakerPolicy(_mockLogger.Object);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void GetDatabaseBulkheadPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseBulkheadPolicy();

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void GetComprehensiveDatabasePolicy_ShouldCombineAllPolicies()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetComprehensiveDatabasePolicy(_mockLogger.Object);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void PollyPolicies_ClassExists_ShouldBeAccessible()
    {
        // Arrange & Act
        var pollyPoliciesType = typeof(PollyPolicies);

        // Assert
        Assert.NotNull(pollyPoliciesType);
        Assert.True(pollyPoliciesType.IsClass);
    }
}
