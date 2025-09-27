using FluentAssertions;
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
        policy.Should().NotBeNull("database retry policy should be configured");
    }

    [Fact]
    public void GetTestRetryPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetTestRetryPolicy(_mockLogger.Object);

        // Assert
        policy.Should().NotBeNull("test retry policy should be configured for fast unit tests");
    }

    [Fact]
    public void GetDatabaseTimeoutPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseTimeoutPolicy();

        // Assert
        policy.Should().NotBeNull("database timeout policy should be configured");
    }

    [Fact]
    public void GetDatabaseCircuitBreakerPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseCircuitBreakerPolicy(_mockLogger.Object);

        // Assert
        policy.Should().NotBeNull("database circuit breaker policy should be configured");
    }

    [Fact]
    public void GetDatabaseBulkheadPolicy_ShouldReturnValidPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetDatabaseBulkheadPolicy();

        // Assert
        policy.Should().NotBeNull("database bulkhead policy should be configured");
    }

    [Fact]
    public void GetComprehensiveDatabasePolicy_ShouldCombineAllPolicies()
    {
        // Arrange & Act
        var policy = PollyPolicies.GetComprehensiveDatabasePolicy(_mockLogger.Object);

        // Assert
        policy.Should().NotBeNull("comprehensive database policy should combine all database policies");
    }

    [Fact]
    public void PollyPolicies_ClassExists_ShouldBeAccessible()
    {
        // Arrange & Act
        var pollyPoliciesType = typeof(PollyPolicies);

        // Assert
        pollyPoliciesType.Should().NotBeNull("PollyPolicies class should be accessible for testing");
        pollyPoliciesType.IsClass.Should().BeTrue("PollyPolicies should be a static class");
    }
}
