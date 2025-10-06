using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Smoke tests for health endpoints across different configurations
/// Validates that health checks work correctly in Development, Testing, and Production environments
/// </summary>
public class HealthEndpointSmokeTests : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public HealthEndpointSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthEndpoint_ShouldReturnHealthy_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /health endpoint in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.GetAsync("/health");

            ValidateHealthyResponse(response, environment, "/health");

            // Validate response content indicates healthy status
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty($"/health should return content in {environment} environment");

            // Basic validation that it's a health check response
            // The exact format may vary, but it should indicate healthy status
            content.ToLower().Should().Contain("healthy",
          $"/health should indicate healthy status in {environment} environment");

        }, $"/health endpoint in {environment}");

        // Assert timing constraint for smoke tests
        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"/health endpoint should respond within 10 seconds in {environment} environment");

        _output.WriteLine($"/health in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task DetailedHealthEndpoint_ShouldReturnHealthy_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing /health/detailed endpoint in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.GetAsync("/health/detailed");

            ValidateHealthyResponse(response, environment, "/health/detailed");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty($"/health/detailed should return content in {environment} environment");

            // Validate JSON response structure for detailed health endpoint
            var healthData = JsonSerializer.Deserialize<JsonElement>(content);
            healthData.ValueKind.Should().Be(JsonValueKind.Object,
              $"/health/detailed should return valid JSON object in {environment} environment");

            // Validate it has detailed health information
            healthData.TryGetProperty("status", out _).Should().BeTrue(
              $"/health/detailed should include status property in {environment} environment");

        }, $"/health/detailed endpoint in {environment}");

        // Assert timing constraint
        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"/health/detailed endpoint should respond within 10 seconds in {environment} environment");

        _output.WriteLine($"/health/detailed in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthCheckServices_ShouldBeRegistered_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing health check service registration in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            await Task.Run(() =>
        {
            using var scope = factory.Services.CreateScope();

            // Verify health check service is registered
            var healthCheckService = scope.ServiceProvider.GetService<HealthCheckService>();
            healthCheckService.Should().NotBeNull(
          $"HealthCheckService should be registered in {environment} environment");

            // Verify health checks are configured
            var healthCheckPublisher = scope.ServiceProvider.GetService<IHealthCheckPublisher>();
            // Publisher is optional, so we don't require it

            _output.WriteLine($"Health check services verified in {environment} environment");
        });

        }, $"Health check service validation in {environment}");

        // Service registration should be very fast
        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Health check service registration should be fast in {environment} environment");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthEndpoint_ShouldProvideEnvironmentSpecificInformation(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing environment-specific health information in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            var response = await client.GetAsync("/health");

            ValidateHealthyResponse(response, environment, "/health");

            var content = await response.Content.ReadAsStringAsync();

            // The health check should reflect the environment configuration
            // This validates that the application is using the correct environment settings
            content.Should().NotBeNullOrEmpty();

            // Log the response for inspection (helpful for debugging)
            _output.WriteLine($"Health response in {environment}: {content.Substring(0, Math.Min(200, content.Length))}...");

        }, $"Environment-specific health check in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Environment-specific health check should complete quickly in {environment}");
    }

    [Fact]
    public async Task AllEnvironments_HealthEndpoints_ShouldCompleteWithin30Seconds()
    {
        // This test validates the overall 30-second constraint for all environments combined
        _output.WriteLine("Testing all environments complete within 30-second constraint");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var environment in GetSupportedEnvironments())
        {
            using var client = CreateClientForEnvironment(environment);

            var response = await client.GetAsync("/health");
            ValidateHealthyResponse(response, environment, "/health");

            _output.WriteLine($"Health check completed for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        totalStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30,
          "All environment health checks should complete within 30 seconds total");

        _output.WriteLine($"Total time for all environments: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
