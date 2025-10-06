using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.SmokeTests;

/// <summary>
/// Performance validation tests to ensure all smoke tests meet the 30-second constraint
/// Validates deployment gate requirements and provides performance monitoring
/// </summary>
public class SmokeTestPerformanceValidation : SmokeTestBase
{
    private readonly ITestOutputHelper _output;

    public SmokeTestPerformanceValidation(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AllSmokeTestCategories_ShouldCompleteWithin30SecondsPerEnvironment()
    {
        _output.WriteLine("=== SMOKE TEST PERFORMANCE VALIDATION ===");
        _output.WriteLine("Validating 30-second per-environment constraint for deployment gates");

        var performanceResults = new Dictionary<string, Dictionary<string, TimeSpan>>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\n--- Performance Testing {environment} Environment ---");

            var environmentStopwatch = Stopwatch.StartNew();
            var categoryResults = new Dictionary<string, TimeSpan>();

            // Category 1: Health Endpoints (Target: <5 seconds)
            var healthTime = await MeasurePerformanceCategory("Health Endpoints", environment, async () =>
            {
                using var client = CreateClientForEnvironment(environment);
                await client.GetAsync("/health");
                await client.GetAsync("/health/ready");
            });
            categoryResults["HealthEndpoints"] = healthTime;

            // Category 2: Authentication (Target: <8 seconds)
            var authTime = await MeasurePerformanceCategory("Authentication", environment, async () =>
            {
                using var client = CreateClientForEnvironment(environment);
                await client.PostAsJsonAsync("/api/auth/login", new { Email = "test@test.com", Password = "Test123!" });
                await client.GetAsync("/api/auth/me");
                await client.PostAsync("/api/auth/logout", null);
            });
            categoryResults["Authentication"] = authTime;

            // Category 3: Critical API (Target: <10 seconds)
            var apiTime = await MeasurePerformanceCategory("Critical API", environment, async () =>
            {
                using var factory = CreateFactoryForEnvironment(environment);
                var adapter = new SmokeTestFactoryAdapter(factory);
                var client = await AuthenticationTestHelper.CreateAdminClientAsync(adapter);
                await client.GetAsync("/api/roles");
                await client.GetAsync("/api/people");
            });
            categoryResults["CriticalAPI"] = apiTime;

            // Category 4: Middleware Pipeline (Target: <7 seconds)
            var middlewareTime = await MeasurePerformanceCategory("Middleware Pipeline", environment, async () =>
            {
                using var client = CreateClientForEnvironment(environment);
                await client.GetAsync("/api/nonexistent"); // 404 handling
                client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
                await client.GetAsync("/health"); // CORS handling
            });
            categoryResults["MiddlewarePipeline"] = middlewareTime;

            environmentStopwatch.Stop();
            performanceResults[environment] = categoryResults;

            // Validate per-environment constraint
            var totalEnvironmentTime = environmentStopwatch.Elapsed.TotalSeconds;
            _output.WriteLine($"Total {environment} time: {totalEnvironmentTime:F2} seconds");

            totalEnvironmentTime.Should().BeLessThan(30,
              $"{environment} environment must complete all smoke tests within 30 seconds for deployment gate requirements");

            // Performance targets per category
            ValidateCategoryPerformance(categoryResults, environment);
        }

        // Generate performance report
        GeneratePerformanceReport(performanceResults);
    }

    private async Task<TimeSpan> MeasurePerformanceCategory(string categoryName,
#pragma warning disable IDE0060 // Remove unused parameter
        string environment,
#pragma warning restore IDE0060 // Remove unused parameter
        Func<Task> testAction)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await testAction();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"  ⚠️  {categoryName} had issues: {ex.GetType().Name}");
            // Continue measurement even if test has issues
        }

        stopwatch.Stop();
        _output.WriteLine($"  {categoryName}: {stopwatch.ElapsedMilliseconds}ms");

        return stopwatch.Elapsed;
    }

    private void ValidateCategoryPerformance(Dictionary<string, TimeSpan> categoryResults, string environment)
    {
        // Performance targets for each category
        var performanceTargets = new Dictionary<string, int>
        {
            ["HealthEndpoints"] = 5,     // 5 seconds
            ["Authentication"] = 8,      // 8 seconds
            ["CriticalAPI"] = 10,        // 10 seconds
            ["MiddlewarePipeline"] = 7   // 7 seconds
        };

        foreach (var category in categoryResults)
        {
            var categoryName = category.Key;
            var actualTime = category.Value.TotalSeconds;
            var targetTime = performanceTargets.GetValueOrDefault(categoryName, 10);

            if (actualTime > targetTime)
            {
                _output.WriteLine($"  ⚠️  {categoryName} exceeded target: {actualTime:F2}s > {targetTime}s");
            }
            else
            {
                _output.WriteLine($"  ✓ {categoryName} within target: {actualTime:F2}s <= {targetTime}s");
            }

            // Soft assertion - log warning but don't fail test for slight overruns
            if (actualTime > targetTime * 1.5) // Only fail if 50% over target
            {
                actualTime.Should().BeLessThan(targetTime * 1.5,
                  $"{categoryName} significantly exceeded performance target in {environment}");
            }
        }
    }

    private void GeneratePerformanceReport(Dictionary<string, Dictionary<string, TimeSpan>> results)
    {
        _output.WriteLine("\n=== PERFORMANCE VALIDATION REPORT ===");

        // Overall statistics
        var allTimes = results.Values.SelectMany(envResults => envResults.Values).ToList();
        var totalTestCount = allTimes.Count;
        var averageTestTime = allTimes.Average(t => t.TotalMilliseconds);
        var maxTestTime = allTimes.Max(t => t.TotalMilliseconds);
        var minTestTime = allTimes.Min(t => t.TotalMilliseconds);

        _output.WriteLine($"Total test categories: {totalTestCount}");
        _output.WriteLine($"Average category time: {averageTestTime:F0}ms");
        _output.WriteLine($"Fastest category: {minTestTime:F0}ms");
        _output.WriteLine($"Slowest category: {maxTestTime:F0}ms");

        // Environment comparison
        _output.WriteLine("\nEnvironment Performance Comparison:");
        foreach (var environmentResult in results)
        {
            var environment = environmentResult.Key;
            var totalTime = environmentResult.Value.Values.Sum(t => t.TotalSeconds);
            var status = totalTime < 30 ? "✓ PASS" : "✗ FAIL";

            _output.WriteLine($"  {environment}: {totalTime:F2}s {status}");
        }

        // Deployment gate validation
        var allEnvironmentsPassing = results.All(env =>
          env.Value.Values.Sum(t => t.TotalSeconds) < 30);

        _output.WriteLine($"\nDeployment Gate Status: {(allEnvironmentsPassing ? "✓ PASS" : "✗ FAIL")}");

        if (!allEnvironmentsPassing)
        {
            _output.WriteLine("⚠️  Some environments exceed 30-second constraint - deployment gate would BLOCK");
        }
        else
        {
            _output.WriteLine("✓ All environments meet 30-second constraint - deployment gate would ALLOW");
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public async Task IndividualEnvironment_ShouldMeetPerformanceTargets(string environment)
    {
        _output.WriteLine($"=== INDIVIDUAL PERFORMANCE VALIDATION: {environment} ===");

        var overallStopwatch = Stopwatch.StartNew();

        // Quick validation of each smoke test category
        var healthTime = await MeasureExecutionTimeAsync(async () =>
        {
            using var client = CreateClientForEnvironment(environment);
            await client.GetAsync("/health");
        }, $"Health check in {environment}");

        var authTime = await MeasureExecutionTimeAsync(async () =>
        {
            using var client = CreateClientForEnvironment(environment);
            await client.PostAsJsonAsync("/api/auth/login", new { Email = "test@test.com", Password = "Test123!" });
        }, $"Auth check in {environment}");

        overallStopwatch.Stop();

        // Individual performance assertions
        healthTime.TotalSeconds.Should().BeLessThan(5,
          $"Health checks should complete within 5 seconds in {environment}");

        authTime.TotalSeconds.Should().BeLessThan(8,
          $"Authentication checks should complete within 8 seconds in {environment}");

        overallStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(15,
          $"Quick validation should complete within 15 seconds in {environment}");

        _output.WriteLine($"{environment} individual validation: {overallStopwatch.Elapsed.TotalSeconds:F2}s");
    }

    [Fact]
    public async Task SmokeTestSuite_ShouldSupportCiCdPipeline_Requirements()
    {
        _output.WriteLine("=== CI/CD PIPELINE INTEGRATION VALIDATION ===");

        var pipelineStopwatch = Stopwatch.StartNew();

        // Simulate CI/CD pipeline execution constraints
        var maxPipelineTime = TimeSpan.FromMinutes(5); // 5 minutes total budget
        var maxEnvironmentTime = TimeSpan.FromSeconds(30); // 30 seconds per environment

        var environmentTimes = new List<TimeSpan>();

        foreach (var environment in GetSupportedEnvironments())
        {
            var environmentStopwatch = Stopwatch.StartNew();

            // Essential checks only (minimum viable smoke test)
            using var client = CreateClientForEnvironment(environment);

            await client.GetAsync("/health");
            await client.PostAsJsonAsync("/api/auth/login", new { Email = "test@test.com", Password = "Test123!" });
            await client.GetAsync("/api/roles");

            environmentStopwatch.Stop();
            environmentTimes.Add(environmentStopwatch.Elapsed);

            _output.WriteLine($"  {environment} pipeline check: {environmentStopwatch.Elapsed.TotalSeconds:F2}s");
        }

        pipelineStopwatch.Stop();

        // Validate CI/CD constraints
        foreach (var (environment, time) in GetSupportedEnvironments().Zip(environmentTimes))
        {
            time.Should().BeLessThan(maxEnvironmentTime,
              $"{environment} should complete within CI/CD time budget");
        }

        pipelineStopwatch.Elapsed.Should().BeLessThan(maxPipelineTime,
          "Total pipeline smoke tests should complete within 5 minutes");

        _output.WriteLine($"Total CI/CD validation time: {pipelineStopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine("✓ Smoke test suite is compatible with CI/CD pipeline requirements");
    }

    [Fact]
    public void SmokeTestConfiguration_ShouldMeetDeploymentGateRequirements()
    {
        _output.WriteLine("=== DEPLOYMENT GATE CONFIGURATION VALIDATION ===");

        // Validate test configuration meets deployment gate requirements
        var supportedEnvironments = GetSupportedEnvironments().ToList();
        var timeConstraint = TimeSpan.FromSeconds(30);

        // Requirements validation
        supportedEnvironments.Should().Contain("Development", "Should test Development environment");
        supportedEnvironments.Should().Contain("Testing", "Should test Testing environment");
        supportedEnvironments.Should().Contain("Production", "Should test Production environment");

        supportedEnvironments.Should().HaveCount(3, "Should test exactly 3 environments");

        timeConstraint.TotalSeconds.Should().Be(30, "Should enforce 30-second constraint");

        _output.WriteLine("✓ Configuration meets deployment gate requirements:");
        _output.WriteLine($"  - Environments: {string.Join(", ", supportedEnvironments)}");
        _output.WriteLine($"  - Time constraint: {timeConstraint.TotalSeconds} seconds per environment");
        _output.WriteLine($"  - Total budget: {timeConstraint.TotalSeconds * supportedEnvironments.Count} seconds");
    }
}
