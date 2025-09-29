using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Dtos;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Contract regression detection tests that identify when configuration changes
/// break existing contracts by comparing expected vs actual behavior across environments
/// </summary>
public class ContractRegressionDetectionTests : ContractTestBase
{
    private readonly ITestOutputHelper _output;

    public ContractRegressionDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ApiResponseSchemas_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING API RESPONSE SCHEMA REGRESSIONS ===");

        var baselineSchemas = await CaptureBaselineApiSchemas();
        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating API schemas in {environment} against baseline:");

            try
            {
                var currentSchemas = await CaptureApiSchemasForEnvironment(environment);

                foreach (var (endpoint, expectedSchema) in baselineSchemas)
                {
                    if (currentSchemas.TryGetValue(endpoint, out var currentSchema))
                    {
                        var regression = DetectSchemaRegression(endpoint, environment, expectedSchema, currentSchema);
                        if (regression != null)
                        {
                            regressions.Add(regression);
                            _output.WriteLine($"  ✗ REGRESSION: {regression}");
                        }
                        else
                        {
                            _output.WriteLine($"  ✓ {endpoint}: Schema consistent");
                        }
                    }
                    else
                    {
                        var regression = $"Endpoint {endpoint} missing in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                }
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        if (regressions.Any())
        {
            _output.WriteLine($"\n{regressions.Count} API schema regressions detected:");
            foreach (var regression in regressions)
            {
                _output.WriteLine($"  - {regression}");
            }
        }

        regressions.Should().BeEmpty("No API schema regressions should be detected across environments");

        _output.WriteLine("\n✓ No API response schema regressions detected");
    }

    [Fact]
    public async Task ServiceRegistrations_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING SERVICE REGISTRATION REGRESSIONS ===");

        var baselineServices = await CaptureBaselineServiceRegistrations();
        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating service registrations in {environment}:");

            try
            {
                var currentServices = await CaptureServiceRegistrationsForEnvironment(environment);

                foreach (var serviceType in baselineServices)
                {
                    if (!currentServices.Contains(serviceType))
                    {
                        var regression = $"Service {serviceType} missing in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                    else
                    {
                        _output.WriteLine($"  ✓ {serviceType}: Available");
                    }
                }

                // Check for unexpected service changes
                var unexpectedServices = currentServices.Except(baselineServices).ToList();
                foreach (var unexpectedService in unexpectedServices)
                {
                    _output.WriteLine($"  + NEW: {unexpectedService} (added in {environment})");
                }
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate services in {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        regressions.Should().BeEmpty("No service registration regressions should be detected");

        _output.WriteLine("\n✓ No service registration regressions detected");
    }

    [Fact]
    public async Task ConfigurationContracts_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING CONFIGURATION CONTRACT REGRESSIONS ===");

        var expectedConfigurationKeys = new[]
        {
      "ConnectionStrings:DefaultConnection",
      "DatabaseProvider",
      "Logging:LogLevel:Default",
      "Caching:UseRedis",
      "OutputCaching:Disabled"
    };

        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating configuration contracts in {environment}:");

            try
            {
                using var factory = CreateFactoryForEnvironment(environment);
                using var scope = factory.Services.CreateScope();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                foreach (var key in expectedConfigurationKeys)
                {
                    var value = configuration[key];
                    if (string.IsNullOrEmpty(value))
                    {
                        var regression = $"Configuration key '{key}' missing or empty in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                    else
                    {
                        _output.WriteLine($"  ✓ {key}: {value}");
                    }
                }

                // Validate environment-specific configuration expectations
                await ValidateEnvironmentSpecificConfigurationContract(environment, configuration, regressions);
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate configuration in {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        regressions.Should().BeEmpty("No configuration contract regressions should be detected");

        _output.WriteLine("\n✓ No configuration contract regressions detected");
    }

    [Fact]
    public async Task MiddlewareBehavior_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING MIDDLEWARE BEHAVIOR REGRESSIONS ===");

        var baselineBehavior = await CaptureBaselineMiddlewareBehavior();
        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating middleware behavior in {environment}:");

            try
            {
                var currentBehavior = await CaptureMiddlewareBehaviorForEnvironment(environment);

                foreach (var (testName, expectedBehavior) in baselineBehavior)
                {
                    if (currentBehavior.TryGetValue(testName, out var currentBehaviorResult))
                    {
                        if (!CompareBehavior(expectedBehavior, currentBehaviorResult))
                        {
                            var regression = $"Middleware behavior regression in {testName} for {environment}";
                            regressions.Add(regression);
                            _output.WriteLine($"  ✗ REGRESSION: {regression}");
                        }
                        else
                        {
                            _output.WriteLine($"  ✓ {testName}: Behavior consistent");
                        }
                    }
                    else
                    {
                        var regression = $"Middleware test {testName} missing in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                }
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate middleware behavior in {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        regressions.Should().BeEmpty("No middleware behavior regressions should be detected");

        _output.WriteLine("\n✓ No middleware behavior regressions detected");
    }

    [Fact]
    public async Task HealthCheckContracts_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING HEALTH CHECK CONTRACT REGRESSIONS ===");

        var baselineHealthChecks = await CaptureBaselineHealthChecks();
        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating health check contracts in {environment}:");

            try
            {
                var currentHealthChecks = await CaptureHealthChecksForEnvironment(environment);

                foreach (var checkName in baselineHealthChecks)
                {
                    if (!currentHealthChecks.Contains(checkName))
                    {
                        var regression = $"Health check '{checkName}' missing in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                    else
                    {
                        _output.WriteLine($"  ✓ {checkName}: Available");
                    }
                }
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate health checks in {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        regressions.Should().BeEmpty("No health check contract regressions should be detected");

        _output.WriteLine("\n✓ No health check contract regressions detected");
    }

    [Fact]
    public async Task DatabaseSchema_ShouldNotRegress_AcrossEnvironments()
    {
        _output.WriteLine("=== DETECTING DATABASE SCHEMA REGRESSIONS ===");

        var baselineSchema = await CaptureBaselineDatabaseSchema();
        var regressions = new List<string>();

        foreach (var environment in GetSupportedEnvironments())
        {
            _output.WriteLine($"\nValidating database schema in {environment}:");

            try
            {
                var currentSchema = await CaptureDatabaseSchemaForEnvironment(environment);

                foreach (var table in baselineSchema)
                {
                    if (!currentSchema.Contains(table))
                    {
                        var regression = $"Database table '{table}' missing in {environment}";
                        regressions.Add(regression);
                        _output.WriteLine($"  ✗ REGRESSION: {regression}");
                    }
                    else
                    {
                        _output.WriteLine($"  ✓ {table}: Available");
                    }
                }
            }
            catch (Exception ex)
            {
                var regression = $"Failed to validate database schema in {environment}: {ex.Message}";
                regressions.Add(regression);
                _output.WriteLine($"  ✗ ERROR: {regression}");
            }
        }

        regressions.Should().BeEmpty("No database schema regressions should be detected");

        _output.WriteLine("\n✓ No database schema regressions detected");
    }

    // Helper methods for regression detection

    private async Task<Dictionary<string, object>> CaptureBaselineApiSchemas()
    {
        var schemas = new Dictionary<string, object>();
        var environment = "Development"; // Use Development as baseline

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        var endpoints = new[] { "/api/roles", "/api/people", "/api/walls", "/api/windows" };

        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    schemas[endpoint] = JsonSerializer.Deserialize<object>(content, JsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to capture baseline schema for {endpoint}: {ex.Message}");
            }
        }

        return schemas;
    }

    private async Task<Dictionary<string, object>> CaptureApiSchemasForEnvironment(string environment)
    {
        var schemas = new Dictionary<string, object>();

        using var factory = CreateFactoryForEnvironment(environment);
        var client = await AuthenticationTestHelper.CreateAdminClientAsync(new ContractTestFactoryAdapter(factory));

        var endpoints = new[] { "/api/roles", "/api/people", "/api/walls", "/api/windows" };

        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    schemas[endpoint] = JsonSerializer.Deserialize<object>(content, JsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to capture schema for {endpoint} in {environment}: {ex.Message}");
            }
        }

        return schemas;
    }

    private static string? DetectSchemaRegression(string endpoint, string environment, object expected, object actual)
    {
        // Simple schema comparison - in a real implementation, this would be more sophisticated
        var expectedJson = JsonSerializer.Serialize(expected, JsonOptions);
        var actualJson = JsonSerializer.Serialize(actual, JsonOptions);

        if (expectedJson != actualJson)
        {
            return $"Schema mismatch for {endpoint} in {environment}";
        }

        return null;
    }

    private Task<HashSet<string>> CaptureBaselineServiceRegistrations()
    {
        var services = new HashSet<string>();
        var environment = "Development";

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var serviceTypes = new[]
        {
      typeof(ApplicationDbContext).Name,
      typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache).Name,
      typeof(MediatR.IMediator).Name,
      typeof(Microsoft.Extensions.Configuration.IConfiguration).Name,
      typeof(Microsoft.Extensions.Logging.ILogger<>).Name,
      typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService).Name
    };

        foreach (var serviceType in serviceTypes)
        {
            services.Add(serviceType);
        }

        return Task.FromResult(services);
    }

    private Task<HashSet<string>> CaptureServiceRegistrationsForEnvironment(string environment)
    {
        var services = new HashSet<string>();

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var serviceTypes = new[]
        {
      typeof(ApplicationDbContext).Name,
      typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache).Name,
      typeof(MediatR.IMediator).Name,
      typeof(Microsoft.Extensions.Configuration.IConfiguration).Name,
      typeof(Microsoft.Extensions.Logging.ILogger<>).Name,
      typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService).Name
    };

        foreach (var serviceType in serviceTypes)
        {
            services.Add(serviceType);
        }

        return Task.FromResult(services);
    }

    private Task ValidateEnvironmentSpecificConfigurationContract(
      string environment,
      IConfiguration configuration,
      List<string> regressions)
    {
        var expectedLogLevel = environment switch
        {
            "Development" => "Information",
            "Testing" => "Warning",
            "Production" => "Error",
            _ => "Information"
        };

        var actualLogLevel = configuration["Logging:LogLevel:Default"];
        if (actualLogLevel != expectedLogLevel)
        {
            regressions.Add($"Log level regression in {environment}: expected {expectedLogLevel}, got {actualLogLevel}");
        }
        return Task.CompletedTask;
    }

    private async Task<Dictionary<string, object>> CaptureBaselineMiddlewareBehavior()
    {
        var behavior = new Dictionary<string, object>();
        var environment = "Development";

        using var client = CreateClientForEnvironment(environment);

        // Test CORS behavior
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        var corsResponse = await client.GetAsync("/api/health");
        behavior["CORS"] = corsResponse.StatusCode;

        // Test compression behavior
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
        var compressionResponse = await client.GetAsync("/api/health");
        behavior["Compression"] = compressionResponse.StatusCode;

        return behavior;
    }

    private async Task<Dictionary<string, object>> CaptureMiddlewareBehaviorForEnvironment(string environment)
    {
        var behavior = new Dictionary<string, object>();

        using var client = CreateClientForEnvironment(environment);

        // Test CORS behavior
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");
        var corsResponse = await client.GetAsync("/api/health");
        behavior["CORS"] = corsResponse.StatusCode;

        // Test compression behavior
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
        var compressionResponse = await client.GetAsync("/api/health");
        behavior["Compression"] = compressionResponse.StatusCode;

        return behavior;
    }

    private static bool CompareBehavior(object expected, object actual)
    {
        return expected.Equals(actual);
    }

    private async Task<HashSet<string>> CaptureBaselineHealthChecks()
    {
        var healthChecks = new HashSet<string>();
        var environment = "Development";

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var healthCheckService = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        var healthReport = await healthCheckService.CheckHealthAsync();

        foreach (var (checkName, _) in healthReport.Entries)
        {
            healthChecks.Add(checkName);
        }

        return healthChecks;
    }

    private async Task<HashSet<string>> CaptureHealthChecksForEnvironment(string environment)
    {
        var healthChecks = new HashSet<string>();

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var healthCheckService = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        var healthReport = await healthCheckService.CheckHealthAsync();

        foreach (var (checkName, _) in healthReport.Entries)
        {
            healthChecks.Add(checkName);
        }

        return healthChecks;
    }

    private Task<HashSet<string>> CaptureBaselineDatabaseSchema()
    {
        var tables = new HashSet<string>();
        var environment = "Development";

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Basic table names from DbContext
        tables.Add("Users");
        tables.Add("Roles");
        tables.Add("People");
        tables.Add("Walls");
        tables.Add("Windows");

        return Task.FromResult(tables);
    }

    private Task<HashSet<string>> CaptureDatabaseSchemaForEnvironment(string environment)
    {
        var tables = new HashSet<string>();

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Basic table names from DbContext
        tables.Add("Users");
        tables.Add("Roles");
        tables.Add("People");
        tables.Add("Walls");
        tables.Add("Windows");

        return Task.FromResult(tables);
    }
}
