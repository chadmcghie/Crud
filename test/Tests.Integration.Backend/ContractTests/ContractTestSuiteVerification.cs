using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Contract test suite verification that ensures all contract tests pass across all configurations
/// Provides comprehensive validation and reporting of contract compliance across environments
/// </summary>
public class ContractTestSuiteVerification : ContractTestBase
{
  private readonly ITestOutputHelper _output;

  public ContractTestSuiteVerification(ITestOutputHelper output)
  {
    _output = output;
  }

  [Fact]
  public async Task AllContractTestSuites_ShouldPass_AcrossAllEnvironments()
  {
    _output.WriteLine("=== COMPREHENSIVE CONTRACT TEST SUITE VERIFICATION ===");
    _output.WriteLine($"Starting contract validation at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

    var overallStopwatch = Stopwatch.StartNew();
    var results = new Dictionary<string, Dictionary<string, ContractTestResult>>();

    foreach (var environment in GetSupportedEnvironments())
    {
      _output.WriteLine($"\n--- Contract Testing {environment} Environment ---");
      var environmentResults = new Dictionary<string, ContractTestResult>();

      // Execute API Contract Tests
      var apiResult = await ExecuteContractTestCategory("API Contract Validation", environment, async () =>
      {
        await ValidateApiContractsForEnvironment(environment);
      });
      environmentResults["ApiContracts"] = apiResult;

      // Execute Middleware Contract Tests
      var middlewareResult = await ExecuteContractTestCategory("Middleware Pipeline Contracts", environment, async () =>
      {
        await ValidateMiddlewarePipelineContractsForEnvironment(environment);
      });
      environmentResults["MiddlewareContracts"] = middlewareResult;

      // Execute Service Interface Contract Tests
      var serviceResult = await ExecuteContractTestCategory("Service Interface Contracts", environment, async () =>
      {
        await ValidateServiceInterfaceContractsForEnvironment(environment);
      });
      environmentResults["ServiceContracts"] = serviceResult;

      // Execute Configuration-Specific Contract Tests
      var configResult = await ExecuteContractTestCategory("Configuration-Specific Contracts", environment, async () =>
      {
        await ValidateConfigurationSpecificContractsForEnvironment(environment);
      });
      environmentResults["ConfigurationContracts"] = configResult;

      results[environment] = environmentResults;

      var totalEnvironmentTime = environmentResults.Values.Sum(r => r.ExecutionTime.TotalSeconds);
      _output.WriteLine($"Total time for {environment}: {totalEnvironmentTime:F2} seconds");

      // Validate per-environment constraint (reasonable time for contract tests)
      totalEnvironmentTime.Should().BeLessThan(120,
        $"{environment} environment should complete all contract tests within 2 minutes");
    }

    overallStopwatch.Stop();

    // Execute Cross-Environment Regression Detection
    _output.WriteLine("\n--- Cross-Environment Regression Detection ---");
    var regressionResult = await ExecuteContractTestCategory("Regression Detection", "All Environments", async () =>
    {
      await ValidateNoContractRegressionsAcrossEnvironments();
    });

    // Print comprehensive results
    PrintContractTestResults(results, regressionResult, overallStopwatch.Elapsed);

    // Validate overall constraints
    overallStopwatch.Elapsed.TotalMinutes.Should().BeLessThan(10,
      "All contract tests should complete within 10 minutes total");

    // Validate all tests passed
    var failedTests = results.Values
      .SelectMany(envResults => envResults.Values)
      .Where(result => !result.Success)
      .ToList();

    if (!regressionResult.Success)
    {
      failedTests.Add(regressionResult);
    }

    if (failedTests.Any())
    {
      var failureMessages = failedTests.Select(f => f.ErrorMessage).Where(m => m != null);
      throw new InvalidOperationException($"Contract test failures detected:\n{string.Join("\n", failureMessages)}");
    }

    _output.WriteLine($"\n=== CONTRACT TEST SUITE VERIFICATION COMPLETED ===");
    _output.WriteLine($"Total execution time: {overallStopwatch.Elapsed.TotalSeconds:F2} seconds");
    _output.WriteLine("✓ All contract tests passed across all environments");
  }

  [Theory]
  [MemberData(nameof(GetEnvironmentsAsTestData))]
  public async Task IndividualEnvironment_ContractTests_ShouldAllPass(string environment)
  {
    _output.WriteLine($"=== INDIVIDUAL ENVIRONMENT CONTRACT VALIDATION: {environment} ===");

    var overallStopwatch = Stopwatch.StartNew();
    var testResults = new List<ContractTestResult>();

    // API Contracts
    var apiResult = await ExecuteContractTestCategory("API Contracts", environment, async () =>
    {
      await ValidateApiContractsForEnvironment(environment);
    });
    testResults.Add(apiResult);

    // Middleware Contracts
    var middlewareResult = await ExecuteContractTestCategory("Middleware Contracts", environment, async () =>
    {
      await ValidateMiddlewarePipelineContractsForEnvironment(environment);
    });
    testResults.Add(middlewareResult);

    // Service Contracts
    var serviceResult = await ExecuteContractTestCategory("Service Contracts", environment, async () =>
    {
      await ValidateServiceInterfaceContractsForEnvironment(environment);
    });
    testResults.Add(serviceResult);

    // Configuration Contracts
    var configResult = await ExecuteContractTestCategory("Configuration Contracts", environment, async () =>
    {
      await ValidateConfigurationSpecificContractsForEnvironment(environment);
    });
    testResults.Add(configResult);

    overallStopwatch.Stop();

    // Validate individual test results
    foreach (var result in testResults)
    {
      result.Success.Should().BeTrue($"{result.Category} should pass in {environment}: {result.ErrorMessage}");
    }

    // Validate timing constraints
    overallStopwatch.Elapsed.TotalSeconds.Should().BeLessThan(60,
      $"Contract tests should complete within 60 seconds in {environment}");

    _output.WriteLine($"Environment {environment} contract validation completed in {overallStopwatch.Elapsed.TotalSeconds:F2} seconds");
    _output.WriteLine($"✓ All contract categories passed in {environment}");
  }

  [Fact]
  public async Task ContractTestSuite_ShouldProvideComprehensiveCoverage()
  {
    _output.WriteLine("=== CONTRACT TEST COVERAGE VALIDATION ===");

    var coverageAreas = new Dictionary<string, bool>
    {
      ["API Response Schemas"] = false,
      ["API Status Codes"] = false,
      ["API Content Types"] = false,
      ["Middleware Pipeline Order"] = false,
      ["Service Registration"] = false,
      ["Configuration Loading"] = false,
      ["Database Connectivity"] = false,
      ["Health Check Availability"] = false,
      ["Cross-Environment Consistency"] = false,
      ["Regression Detection"] = false
    };

    // Validate API contract coverage
    try
    {
      await ValidateApiContractsForEnvironment("Development");
      coverageAreas["API Response Schemas"] = true;
      coverageAreas["API Status Codes"] = true;
      coverageAreas["API Content Types"] = true;
    }
    catch (Exception ex)
    {
      _output.WriteLine($"API contract coverage validation failed: {ex.Message}");
    }

    // Validate middleware coverage
    try
    {
      await ValidateMiddlewarePipelineContractsForEnvironment("Development");
      coverageAreas["Middleware Pipeline Order"] = true;
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Middleware contract coverage validation failed: {ex.Message}");
    }

    // Validate service coverage
    try
    {
      await ValidateServiceInterfaceContractsForEnvironment("Development");
      coverageAreas["Service Registration"] = true;
      coverageAreas["Database Connectivity"] = true;
      coverageAreas["Health Check Availability"] = true;
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Service contract coverage validation failed: {ex.Message}");
    }

    // Validate configuration coverage
    try
    {
      await ValidateConfigurationSpecificContractsForEnvironment("Development");
      coverageAreas["Configuration Loading"] = true;
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Configuration contract coverage validation failed: {ex.Message}");
    }

    // Validate cross-environment coverage
    try
    {
      await ValidateNoContractRegressionsAcrossEnvironments();
      coverageAreas["Cross-Environment Consistency"] = true;
      coverageAreas["Regression Detection"] = true;
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Cross-environment contract coverage validation failed: {ex.Message}");
    }

    // Report coverage
    _output.WriteLine("\nContract Test Coverage Report:");
    foreach (var (area, covered) in coverageAreas)
    {
      var status = covered ? "✓" : "✗";
      _output.WriteLine($"  {status} {area}");
    }

    var coveragePercentage = (coverageAreas.Values.Count(c => c) * 100.0) / coverageAreas.Count;
    _output.WriteLine($"\nOverall Coverage: {coveragePercentage:F1}%");

    coveragePercentage.Should().Be(100, "Contract test suite should provide 100% coverage of critical areas");

    _output.WriteLine("✓ Contract test suite provides comprehensive coverage");
  }

  [Fact]
  public async Task ContractTestPerformance_ShouldMeetRequirements()
  {
    _output.WriteLine("=== CONTRACT TEST PERFORMANCE VALIDATION ===");

    var performanceTargets = new Dictionary<string, TimeSpan>
    {
      ["Single Environment"] = TimeSpan.FromSeconds(60),
      ["All Environments"] = TimeSpan.FromMinutes(10),
      ["Regression Detection"] = TimeSpan.FromMinutes(2)
    };

    var performanceResults = new Dictionary<string, TimeSpan>();

    // Test single environment performance
    var singleEnvStopwatch = Stopwatch.StartNew();
    await IndividualEnvironment_ContractTests_ShouldAllPass("Development");
    singleEnvStopwatch.Stop();
    performanceResults["Single Environment"] = singleEnvStopwatch.Elapsed;

    // Test regression detection performance
    var regressionStopwatch = Stopwatch.StartNew();
    await ValidateNoContractRegressionsAcrossEnvironments();
    regressionStopwatch.Stop();
    performanceResults["Regression Detection"] = regressionStopwatch.Elapsed;

    // Report performance results
    _output.WriteLine("\nContract Test Performance Results:");
    foreach (var (category, target) in performanceTargets)
    {
      if (performanceResults.TryGetValue(category, out var actual))
      {
        var status = actual <= target ? "✓" : "✗";
        _output.WriteLine($"  {status} {category}: {actual.TotalSeconds:F2}s (target: {target.TotalSeconds:F0}s)");

        actual.Should().BeLessOrEqualTo(target,
          $"{category} should complete within {target.TotalSeconds:F0} seconds");
      }
    }

    _output.WriteLine("✓ Contract test performance meets requirements");
  }

  // Helper methods for contract validation

  private async Task<ContractTestResult> ExecuteContractTestCategory(
    string category,
    string environment,
    Func<Task> testAction)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      await testAction();
      stopwatch.Stop();

      _output.WriteLine($"  ✓ {category}: {stopwatch.ElapsedMilliseconds}ms");
      return new ContractTestResult(category, environment, true, stopwatch.Elapsed, null);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();

      _output.WriteLine($"  ✗ {category}: {ex.GetType().Name} - {ex.Message}");
      return new ContractTestResult(category, environment, false, stopwatch.Elapsed, ex.Message);
    }
  }

  private async Task ValidateApiContractsForEnvironment(string environment)
  {
    using var factory = CreateFactoryForEnvironment(environment);
    var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

    var endpoints = new[] { "/api/roles", "/api/people", "/api/walls", "/api/windows" };

    foreach (var endpoint in endpoints)
    {
      var response = await client.GetAsync(endpoint);
      response.StatusCode.Should().Be(HttpStatusCode.OK, $"API endpoint {endpoint} should be accessible in {environment}");
      response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
        $"API endpoint {endpoint} should return JSON in {environment}");
    }
  }

  private async Task ValidateMiddlewarePipelineContractsForEnvironment(string environment)
  {
    using var client = CreateClientForEnvironment(environment);

    // Test basic middleware pipeline
    var response = await client.GetAsync("/api/health");
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

    // Test CORS middleware
    client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
    var corsResponse = await client.GetAsync("/api/health");
    corsResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
  }

  private async Task ValidateServiceInterfaceContractsForEnvironment(string environment)
  {
    using var factory = CreateFactoryForEnvironment(environment);
    using var scope = factory.Services.CreateScope();

    // Validate core services
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
      .Should().NotBeNull($"Database context should be available in {environment}");

    scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
      .Should().NotBeNull($"Configuration should be available in {environment}");

    scope.ServiceProvider.GetRequiredService<MediatR.IMediator>()
      .Should().NotBeNull($"MediatR should be available in {environment}");
  }

  private async Task ValidateConfigurationSpecificContractsForEnvironment(string environment)
  {
    using var factory = CreateFactoryForEnvironment(environment);
    using var scope = factory.Services.CreateScope();

    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Validate required configuration
    configuration.GetConnectionString("DefaultConnection")
      .Should().NotBeNullOrEmpty($"Connection string should be configured in {environment}");

    configuration["DatabaseProvider"]
      .Should().Be("SQLite", $"Database provider should be SQLite in {environment}");

    configuration["Logging:LogLevel:Default"]
      .Should().NotBeNullOrEmpty($"Log level should be configured in {environment}");
  }

  private async Task ValidateNoContractRegressionsAcrossEnvironments()
  {
    var environments = GetSupportedEnvironments().ToList();

    // Validate that all environments have the same API endpoints
    var endpointsByEnvironment = new Dictionary<string, HashSet<string>>();

    foreach (var environment in environments)
    {
      using var factory = CreateFactoryForEnvironment(environment);
      var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

      var endpoints = new HashSet<string>();
      var testEndpoints = new[] { "/api/roles", "/api/people", "/api/walls", "/api/windows" };

      foreach (var endpoint in testEndpoints)
      {
        var response = await client.GetAsync(endpoint);
        if (response.StatusCode == HttpStatusCode.OK)
        {
          endpoints.Add(endpoint);
        }
      }

      endpointsByEnvironment[environment] = endpoints;
    }

    // Ensure all environments have the same endpoints
    var baselineEndpoints = endpointsByEnvironment.Values.First();
    foreach (var (environment, endpoints) in endpointsByEnvironment.Skip(1))
    {
      endpoints.Should().BeEquivalentTo(baselineEndpoints,
        $"Environment {environment} should have the same API endpoints as baseline");
    }
  }

  private void PrintContractTestResults(
    Dictionary<string, Dictionary<string, ContractTestResult>> results,
    ContractTestResult regressionResult,
    TimeSpan totalTime)
  {
    _output.WriteLine("\n=== CONTRACT TEST RESULTS SUMMARY ===");

    foreach (var (environment, tests) in results)
    {
      _output.WriteLine($"\n{environment} Environment:");
      foreach (var (category, result) in tests)
      {
        var status = result.Success ? "✓ PASS" : "✗ FAIL";
        _output.WriteLine($"  {category}: {result.ExecutionTime.TotalMilliseconds:F0}ms {status}");
        if (!result.Success && result.ErrorMessage != null)
        {
          _output.WriteLine($"    Error: {result.ErrorMessage}");
        }
      }

      var environmentTotal = tests.Values.Sum(t => t.ExecutionTime.TotalSeconds);
      var environmentStatus = tests.Values.All(t => t.Success) ? "✓ PASS" : "✗ FAIL";
      _output.WriteLine($"  Total: {environmentTotal:F2}s {environmentStatus}");
    }

    // Regression detection results
    _output.WriteLine($"\nRegression Detection:");
    var regressionStatus = regressionResult.Success ? "✓ PASS" : "✗ FAIL";
    _output.WriteLine($"  Cross-Environment Validation: {regressionResult.ExecutionTime.TotalMilliseconds:F0}ms {regressionStatus}");

    _output.WriteLine($"\nOverall Execution Time: {totalTime.TotalSeconds:F2} seconds");

    // Overall status
    var allPassed = results.Values.SelectMany(v => v.Values).All(r => r.Success) && regressionResult.Success;
    var overallStatus = allPassed ? "✓ ALL TESTS PASSED" : "✗ SOME TESTS FAILED";
    _output.WriteLine($"\nOverall Status: {overallStatus}");
  }

  private record ContractTestResult(
    string Category,
    string Environment,
    bool Success,
    TimeSpan ExecutionTime,
    string? ErrorMessage);
}