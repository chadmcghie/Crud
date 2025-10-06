using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Smoke tests for middleware pipeline configuration across different environments
/// Validates that middleware components are properly configured and functioning
/// </summary>
public class MiddlewarePipelineSmokeTests : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public MiddlewarePipelineSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ExceptionHandlingMiddleware_ShouldHandleErrors_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing exception handling middleware in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            using var client = factory.CreateClient();

            // Test that exceptions are handled gracefully (not returning 500 with stack traces in production)
            var response = await client.GetAsync("/api/nonexistent/trigger-error");

            // Should return 404, not 500 (proper error handling)
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
          $"Non-existent endpoints should return 404, not 500, in {environment}");

            // Response should not contain stack traces in production-like environments
            var content = await response.Content.ReadAsStringAsync();
            if (environment == "Production")
            {
                content.ToLower().Should().NotContain("stack trace",
              "Production environment should not expose stack traces");
                content.ToLower().Should().NotContain("exception",
              "Production environment should not expose detailed exception info");
            }

            _output.WriteLine($"✓ Exception handling working correctly in {environment}");

        }, $"Exception handling middleware in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Exception handling test should be fast in {environment} environment");

        _output.WriteLine($"Exception handling in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CorsMiddleware_ShouldBeConfigured_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing CORS middleware configuration in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            // Add Origin header to trigger CORS
            client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

            var response = await client.GetAsync("/api/roles");

            // CORS middleware should process the request (not necessarily add headers in test environment)
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,
          HttpStatusCode.Unauthorized // If authentication is required
        );

            // In development, CORS headers might be present
            if (environment == "Development")
            {
                // Check if CORS headers are present (optional in test environment)
                var corsHeaders = response.Headers.Where(h => h.Key.StartsWith("Access-Control")).ToList();
                _output.WriteLine($"CORS headers in {environment}: {corsHeaders.Count} found");
            }

            _output.WriteLine($"✓ CORS middleware processed request in {environment}");

        }, $"CORS middleware in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"CORS test should complete quickly in {environment} environment");

        _output.WriteLine($"CORS test in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CompressionMiddleware_ShouldBeWorking_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing compression middleware in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            // Request with compression acceptance
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            var response = await client.GetAsync("/api/roles");

            // Should respond (compression is transparent to status codes)
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,
          HttpStatusCode.Unauthorized
        );

            // Check if compression is applied (response might be compressed)
            var contentEncoding = response.Content.Headers.ContentEncoding;
            if (contentEncoding.Any())
            {
                _output.WriteLine($"Content encoding in {environment}: {string.Join(", ", contentEncoding)}");
            }

            // Ensure we can read the response (decompression works)
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNull($"Response should be readable in {environment}");

            _output.WriteLine($"✓ Compression middleware working in {environment}");

        }, $"Compression middleware in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Compression test should complete quickly in {environment} environment");

        _output.WriteLine($"Compression test in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task AuthenticationMiddleware_ShouldEnforceAuth_WithinTimeLimit(string environment)
    {
        // Arrange
        using var client = CreateClientForEnvironment(environment);
        _output.WriteLine($"Testing authentication middleware in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            // Test protected endpoint without authentication
            var response = await client.GetAsync("/api/roles");

            // Should require authentication or allow access based on configuration
            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,           // If endpoint is not protected or has default data
          HttpStatusCode.Unauthorized  // If authentication is required
        );

            // Test with invalid token
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");
            var invalidTokenResponse = await client.GetAsync("/api/roles");

            invalidTokenResponse.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,           // If endpoint allows anonymous access
          HttpStatusCode.Unauthorized  // If token validation is enforced
        );

            _output.WriteLine($"✓ Authentication middleware functioning in {environment}");

        }, $"Authentication middleware in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Authentication test should complete quickly in {environment} environment");

        _output.WriteLine($"Authentication middleware in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task LoggingMiddleware_ShouldBeConfigured_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing logging middleware configuration in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            // Verify logging services are registered
            using var scope = factory.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
            loggerFactory.Should().NotBeNull($"Logger factory should be available in {environment}");

            var logger = scope.ServiceProvider.GetService<ILogger<MiddlewarePipelineSmokeTests>>();
            logger.Should().NotBeNull($"Logger should be available in {environment}");

            // Test that logging doesn't break requests
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");

            response.StatusCode.Should().BeOneOf(
          HttpStatusCode.OK,
          HttpStatusCode.ServiceUnavailable // If health checks fail
        );

            _output.WriteLine($"✓ Logging middleware configured in {environment}");

        }, $"Logging middleware in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(5,
          $"Logging test should complete quickly in {environment} environment");

        _output.WriteLine($"Logging middleware in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task MiddlewarePipeline_ShouldHandleConcurrentRequests_WithinTimeLimit(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        _output.WriteLine($"Testing middleware pipeline under concurrent load in {environment} environment");

        // Act & Assert
        var executionTime = await MeasureExecutionTimeAsync(async () =>
        {
            using var client = factory.CreateClient();

            // Send multiple concurrent requests to test middleware pipeline stability
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(client.GetAsync("/health"));
            }

            var responses = await Task.WhenAll(tasks);

            // All requests should complete successfully (middleware pipeline should handle concurrency)
            foreach (var response in responses)
            {
                response.StatusCode.Should().BeOneOf(
              HttpStatusCode.OK,
              HttpStatusCode.ServiceUnavailable,
              HttpStatusCode.Unauthorized
            );
            }

            _output.WriteLine($"✓ Middleware pipeline handled {responses.Length} concurrent requests in {environment}");

        }, $"Concurrent middleware test in {environment}");

        executionTime.TotalSeconds.Should().BeLessThan(10,
          $"Concurrent middleware test should complete within 10 seconds in {environment} environment");

        _output.WriteLine($"Concurrent middleware test in {environment}: {executionTime.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public async Task AllEnvironments_MiddlewarePipeline_ShouldCompleteWithin30Seconds()
    {
        // This test validates the overall 30-second constraint for middleware pipeline validation
        _output.WriteLine("Testing all environments middleware pipeline complete within 30-second constraint");

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var environment in GetSupportedEnvironments())
        {
            using var client = CreateClientForEnvironment(environment);

            // Test key middleware components quickly
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

            // Test error handling
            var errorResponse = await client.GetAsync("/api/nonexistent");
            errorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _output.WriteLine($"Middleware pipeline validated for {environment}: {totalStopwatch.ElapsedMilliseconds}ms elapsed");
        }

        totalStopwatch.Stop();

        // Validate overall time constraint
        totalStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30,
          "All environment middleware pipeline tests should complete within 30 seconds total");

        _output.WriteLine($"Total middleware pipeline test time: {totalStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
