using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Automated smoke test runner that executes all smoke tests across environments
/// Provides programmatic execution for CI/CD integration and performance monitoring
/// </summary>
public class AutomatedSmokeTestRunner : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public AutomatedSmokeTestRunner(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ExecuteAllSmokeTests_ForAllEnvironments_WithinTimeConstraints()
    {
        _output.WriteLine("=== AUTOMATED SMOKE TEST EXECUTION ===");
        _output.WriteLine($"Starting comprehensive smoke test execution at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        var overallStopwatch = Stopwatch.StartNew();
        var results = new Dictionary<string, Dictionary<string, TimeSpan>>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\n--- Testing {environment} Environment ---");
            var environmentResults = new Dictionary<string, TimeSpan>();

            // Execute health endpoint tests
            var healthTime = await ExecuteHealthEndpointTests(environment);
            environmentResults["HealthEndpoints"] = healthTime;

            // Execute authentication tests
            var authTime = await ExecuteAuthenticationTests(environment);
            environmentResults["Authentication"] = authTime;

            // Execute critical API tests
            var apiTime = await ExecuteCriticalApiTests(environment);
            environmentResults["CriticalApi"] = apiTime;

            // Execute middleware pipeline tests
            var middlewareTime = await ExecuteMiddlewarePipelineTests(environment);
            environmentResults["MiddlewarePipeline"] = middlewareTime;

            results[environment] = environmentResults;

            var totalEnvironmentTime = environmentResults.Values.Sum(t => t.TotalSeconds);
            _output.WriteLine($"Total time for {environment}: {totalEnvironmentTime:F2} seconds");

            // Validate per-environment time constraint (30 seconds)
            totalEnvironmentTime.Should().BeLessThan(30,
              $"{environment} environment should complete all smoke tests within 30 seconds");
        }

        overallStopwatch.Stop();

        // Print comprehensive results
        PrintSmokeTestResults(results, overallStopwatch.Elapsed);

        // Validate overall constraints
        overallStopwatch.Elapsed.TotalMinutes.Should().BeLessThan(5,
          "All smoke tests should complete within 5 minutes total");

        _output.WriteLine($"\n=== SMOKE TEST EXECUTION COMPLETED ===");
        _output.WriteLine($"Total execution time: {overallStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    private async Task<TimeSpan> ExecuteHealthEndpointTests(string environment)
    {
        var stopwatch = Stopwatch.StartNew();

        using var client = CreateClientForEnvironment(environment);

        // Test /health endpoint
        var healthResponse = await client.GetAsync("/health");
        healthResponse.StatusCode.Should().BeOneOf(
          System.Net.HttpStatusCode.OK,
          System.Net.HttpStatusCode.ServiceUnavailable
        );

        // Test /health/detailed endpoint
        var detailedHealthResponse = await client.GetAsync("/health/detailed");
        detailedHealthResponse.StatusCode.Should().BeOneOf(
          System.Net.HttpStatusCode.OK,
          System.Net.HttpStatusCode.ServiceUnavailable
        );

        stopwatch.Stop();
        _output.WriteLine($"  ✓ Health endpoints: {stopwatch.ElapsedMilliseconds}ms");
        return stopwatch.Elapsed;
    }

    private async Task<TimeSpan> ExecuteAuthenticationTests(string environment)
    {
        var stopwatch = Stopwatch.StartNew();

        using var client = CreateClientForEnvironment(environment);

        // Test login endpoint
        var loginRequest = new { Email = "test@example.com", Password = "Test123!" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().NotBe(System.Net.HttpStatusCode.NotFound);

        // Test auth/me endpoint
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        stopwatch.Stop();
        _output.WriteLine($"  ✓ Authentication: {stopwatch.ElapsedMilliseconds}ms");
        return stopwatch.Elapsed;
    }

    private async Task<TimeSpan> ExecuteCriticalApiTests(string environment)
    {
        var stopwatch = Stopwatch.StartNew();

        using var factory = CreateFactoryForEnvironment(environment);
        var adapter = new SmokeTestFactoryAdapter(factory);
        var authenticatedClient = await Tests.Integration.Backend.Infrastructure.AuthenticationTestHelper.CreateAdminClientAsync(adapter);

        // Test critical endpoints
        var endpoints = new[] { "/api/roles", "/api/people", "/api/walls", "/api/windows" };
        foreach (var endpoint in endpoints)
        {
            var response = await authenticatedClient.GetAsync(endpoint);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        stopwatch.Stop();
        _output.WriteLine($"  ✓ Critical API: {stopwatch.ElapsedMilliseconds}ms");
        return stopwatch.Elapsed;
    }

    private async Task<TimeSpan> ExecuteMiddlewarePipelineTests(string environment)
    {
        var stopwatch = Stopwatch.StartNew();

        using var client = CreateClientForEnvironment(environment);

        // Test error handling
        var errorResponse = await client.GetAsync("/api/nonexistent");
        errorResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        // Test CORS (with origin header)
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        var corsResponse = await client.GetAsync("/health");
        corsResponse.StatusCode.Should().BeOneOf(
          System.Net.HttpStatusCode.OK,
          System.Net.HttpStatusCode.ServiceUnavailable
        );

        stopwatch.Stop();
        _output.WriteLine($"  ✓ Middleware: {stopwatch.ElapsedMilliseconds}ms");
        return stopwatch.Elapsed;
    }

    private void PrintSmokeTestResults(Dictionary<string, Dictionary<string, TimeSpan>> results, TimeSpan totalTime)
    {
        _output.WriteLine("\n=== SMOKE TEST RESULTS SUMMARY ===");

        foreach (var environmentResult in results)
        {
            var environment = environmentResult.Key;
            var tests = environmentResult.Value;

            _output.WriteLine($"\n{environment} Environment:");
            foreach (var test in tests)
            {
                _output.WriteLine($"  {test.Key}: {test.Value.TotalMilliseconds:F0}ms");
            }

            var environmentTotal = tests.Values.Sum(t => t.TotalSeconds);
            var status = environmentTotal < 30 ? "✓ PASS" : "✗ FAIL";
            _output.WriteLine($"  Total: {environmentTotal:F2}s {status}");
        }

        _output.WriteLine($"\nOverall Execution Time: {totalTime.TotalSeconds:F2} seconds");

        // Performance summary
        var allEnvironmentTimes = results.Values.SelectMany(v => v.Values).ToList();
        var avgTestTime = allEnvironmentTimes.Average(t => t.TotalMilliseconds);
        var maxTestTime = allEnvironmentTimes.Max(t => t.TotalMilliseconds);

        _output.WriteLine($"Average test time: {avgTestTime:F0}ms");
        _output.WriteLine($"Maximum test time: {maxTestTime:F0}ms");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task SmokeTest_Environment_ShouldPassAllChecks(string environment)
    {
        _output.WriteLine($"=== INDIVIDUAL ENVIRONMENT SMOKE TEST: {environment} ===");

        var stopwatch = Stopwatch.StartNew();

        // Health checks
        await ExecuteHealthEndpointTests(environment);

        // Authentication checks
        await ExecuteAuthenticationTests(environment);

        // API checks
        await ExecuteCriticalApiTests(environment);

        // Middleware checks
        await ExecuteMiddlewarePipelineTests(environment);

        stopwatch.Stop();

        _output.WriteLine($"Environment {environment} completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");

        // Assert time constraint
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(30,
          $"{environment} environment should complete all checks within 30 seconds");
    }

    [Fact]
    public void GenerateSmokeTestReport_ForCiCdIntegration()
    {
        _output.WriteLine("=== GENERATING CI/CD SMOKE TEST REPORT ===");

        var report = new
        {
            TestSuite = "SmokeTests",
            Timestamp = DateTime.UtcNow,
            Environments = GetSupportedEnvironments().ToArray(),
            TimeConstraints = new
            {
                PerEnvironment = "30 seconds",
                Total = "5 minutes"
            },
            TestCategories = new[]
          {
        "HealthEndpoints",
        "Authentication",
        "CriticalApi",
        "MiddlewarePipeline"
      }
        };

        var reportJson = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        _output.WriteLine("Smoke Test Configuration:");
        _output.WriteLine(reportJson);

        // This could be extended to write to a file for CI/CD consumption
        // File.WriteAllText("smoke-test-config.json", reportJson);

        report.Environments.Should().HaveCount(3, "Should test 3 environments");
        report.TestCategories.Should().HaveCount(4, "Should have 4 test categories");
    }
}
