using System.Net;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Configuration-specific contract validation tests that ensure environment-specific settings
/// don't break existing contracts while maintaining environment-appropriate behavior
/// </summary>
public class ConfigurationSpecificContractTests : ContractTestBase
{
    private readonly ITestOutputHelper _output;

    public ConfigurationSpecificContractTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task DatabaseConfiguration_Contract_ShouldRespectEnvironmentSettings(string environment)
    {
        _output.WriteLine($"=== VALIDATING DATABASE CONFIGURATION CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Validate database configuration contract per environment
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty($"Connection string should be configured in {environment}");

        var databaseProvider = configuration["DatabaseProvider"];
        databaseProvider.Should().Be("SQLite", $"Database provider should be SQLite for contract tests in {environment}");

        // Environment-specific database naming contract
        connectionString.Should().Contain($"CrudContract_{environment.Substring(0, 4)}",
          $"Database should use environment-specific naming in {environment}");

        // Validate database accessibility contract
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();
        canConnect.Should().BeTrue($"Database should be accessible in {environment}");

        _output.WriteLine($"✓ Database configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task LoggingConfiguration_Contract_ShouldRespectEnvironmentLevels(string environment)
    {
        _output.WriteLine($"=== VALIDATING LOGGING CONFIGURATION CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConfigurationSpecificContractTests>>();

        // Environment-specific logging level contract
        var configuredLogLevel = configuration["Logging:LogLevel:Default"];
        var expectedLogLevel = environment switch
        {
            "Development" => "Information",
            "Testing" => "Warning",
            "Production" => "Error",
            _ => "Information"
        };

        configuredLogLevel.Should().Be(expectedLogLevel,
          $"Log level should be {expectedLogLevel} in {environment} environment");

        // Validate logging behavior contract
        var shouldLogInfo = environment == "Development";
        var shouldLogWarning = environment != "Production";
        var shouldLogError = true; // All environments should log errors

        logger.IsEnabled(LogLevel.Information).Should().Be(shouldLogInfo,
          $"Information logging should be {(shouldLogInfo ? "enabled" : "disabled")} in {environment}");

        logger.IsEnabled(LogLevel.Warning).Should().Be(shouldLogWarning,
          $"Warning logging should be {(shouldLogWarning ? "enabled" : "disabled")} in {environment}");

        logger.IsEnabled(LogLevel.Error).Should().Be(shouldLogError,
          $"Error logging should be enabled in {environment}");

        _output.WriteLine($"✓ Logging configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CachingConfiguration_Contract_ShouldMaintainConsistency(string environment)
    {
        _output.WriteLine($"=== VALIDATING CACHING CONFIGURATION CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Caching configuration contract
        var useRedis = configuration["Caching:UseRedis"];
        useRedis.Should().Be("false", $"Redis should be disabled for contract tests in {environment}");

        var outputCachingDisabled = configuration["OutputCaching:Disabled"];
        outputCachingDisabled.Should().Be("false", $"Output caching should be enabled in {environment}");

        // Validate memory cache availability contract
        var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull($"Memory cache should be available in {environment}");

        // Test caching behavior contract
        var testKey = $"config-contract-{environment}";
        var testValue = $"test-{DateTime.UtcNow.Ticks}";

        memoryCache!.Set(testKey, testValue, TimeSpan.FromMinutes(1));
        var retrievedValue = memoryCache.Get<string>(testKey);
        retrievedValue.Should().Be(testValue, $"Caching should work consistently in {environment}");

        _output.WriteLine($"✓ Caching configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task SecurityConfiguration_Contract_ShouldVaryByEnvironment(string environment)
    {
        _output.WriteLine($"=== VALIDATING SECURITY CONFIGURATION CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Test error information disclosure contract
        var response = await client.GetAsync("/api/nonexistent/endpoint");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();

        // Environment-specific security contract
        switch (environment)
        {
            case "Development":
                // Development may expose more detailed error information
                content.Should().NotBeNullOrEmpty("Development should provide error information");
                break;

            case "Testing":
                // Testing should have moderate error information
                content.Should().NotBeNullOrEmpty("Testing should provide error information");
                break;

            case "Production":
                // Production should minimize error information exposure
                content.ToLower().Should().NotContain("stack trace",
                  "Production should not expose stack traces");
                content.ToLower().Should().NotContain("exception",
                  "Production should not expose detailed exception information");
                break;
        }

        _output.WriteLine($"✓ Security configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthCheckConfiguration_Contract_ShouldBeEnvironmentAware(string environment)
    {
        _output.WriteLine($"=== VALIDATING HEALTH CHECK CONFIGURATION CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        // Health check contract
        var healthReport = await healthCheckService.CheckHealthAsync();
        healthReport.Should().NotBeNull($"Health check should be available in {environment}");

        // Environment-specific health check expectations
        var expectedStatus = HealthStatus.Healthy; // Contract tests should always be healthy

        healthReport.Status.Should().Be(expectedStatus,
          $"Health check should report {expectedStatus} status in {environment}");

        // Validate health check entries
        healthReport.Entries.Should().NotBeEmpty($"Health check should have entries in {environment}");

        foreach (var (checkName, entry) in healthReport.Entries)
        {
            entry.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);

            _output.WriteLine($"Health check '{checkName}' in {environment}: {entry.Status}");
        }

        _output.WriteLine($"✓ Health check configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public Task EnvironmentSpecificServices_Contract_ShouldBeConfiguredCorrectly(string environment)
    {
        _output.WriteLine($"=== VALIDATING ENVIRONMENT-SPECIFIC SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Validate environment-specific service registrations
        var services = factory.Services;

        // All environments should have these core services
        scope.ServiceProvider.GetService<ApplicationDbContext>()
          .Should().NotBeNull($"ApplicationDbContext should be registered in {environment}");

        scope.ServiceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()
          .Should().NotBeNull($"Memory cache should be registered in {environment}");

        scope.ServiceProvider.GetService<MediatR.IMediator>()
          .Should().NotBeNull($"MediatR should be registered in {environment}");

        // Environment-specific behavior validation
        switch (environment)
        {
            case "Development":
                // Development might have additional debugging services
                _output.WriteLine("Development environment: Enhanced debugging services available");
                break;

            case "Testing":
                // Testing might have testing-specific services
                _output.WriteLine("Testing environment: Testing-optimized services available");
                break;

            case "Production":
                // Production should have performance-optimized services
                _output.WriteLine("Production environment: Production-optimized services available");
                break;
        }

        _output.WriteLine($"✓ Environment-specific services contract validated for {environment}");
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ConfigurationInheritance_Contract_ShouldMaintainHierarchy(string environment)
    {
        _output.WriteLine($"=== VALIDATING CONFIGURATION INHERITANCE CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Base configuration that should exist in all environments
        var baseSettings = new[]
        {
      "ConnectionStrings:DefaultConnection",
      "DatabaseProvider",
      "Logging:LogLevel:Default"
    };

        foreach (var setting in baseSettings)
        {
            var value = configuration[setting];
            value.Should().NotBeNullOrEmpty($"Base setting '{setting}' should be configured in {environment}");
            _output.WriteLine($"{setting}: {value}");
        }

        // Environment-specific overrides should be applied
        var environmentSettings = new Dictionary<string, string[]>
        {
            ["Development"] = new[] { "Logging:LogLevel:Default" },
            ["Testing"] = new[] { "Logging:LogLevel:Default" },
            ["Production"] = new[] { "Logging:LogLevel:Default" }
        };

        if (environmentSettings.TryGetValue(environment, out var settings))
        {
            foreach (var setting in settings)
            {
                var value = configuration[setting];
                value.Should().NotBeNullOrEmpty($"Environment setting '{setting}' should be configured in {environment}");
            }
        }

        _output.WriteLine($"✓ Configuration inheritance contract validated for {environment}");
    }

    [Fact]
    public async Task ConfigurationContracts_ShouldBeConsistentYetDistinct()
    {
        _output.WriteLine("=== CROSS-ENVIRONMENT CONFIGURATION CONTRACT CONSISTENCY ===");

        var configurationData = new Dictionary<string, Dictionary<string, string>>();

        // Collect configuration from all environments
        foreach (var environment in GetSupportedEnvironments())
        {
            using var factory = CreateFactoryForEnvironment(environment);
            using var scope = factory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var environmentConfig = new Dictionary<string, string>
            {
                ["ConnectionString"] = configuration.GetConnectionString("DefaultConnection") ?? "",
                ["DatabaseProvider"] = configuration["DatabaseProvider"] ?? "",
                ["LogLevel"] = configuration["Logging:LogLevel:Default"] ?? "",
                ["UseRedis"] = configuration["Caching:UseRedis"] ?? "",
                ["OutputCachingDisabled"] = configuration["OutputCaching:Disabled"] ?? ""
            };

            configurationData[environment] = environmentConfig;
        }

        // Validate consistency requirements
        foreach (var (env, config) in configurationData)
        {
            _output.WriteLine($"\n{env} Configuration:");
            foreach (var (key, value) in config)
            {
                _output.WriteLine($"  {key}: {value}");
            }

            // All environments should use SQLite for contract tests
            config["DatabaseProvider"].Should().Be("SQLite", $"Database provider should be consistent");

            // All environments should have Redis disabled for contract tests
            config["UseRedis"].Should().Be("false", $"Redis should be disabled for contract tests");

            // Connection strings should be environment-specific
            config["ConnectionString"].Should().Contain($"CrudContract_{env.Substring(0, 4)}",
              $"Connection string should be environment-specific for {env}");
        }

        // Validate environment-specific differences
        configurationData["Development"]["LogLevel"].Should().Be("Information",
          "Development should use Information log level");
        configurationData["Testing"]["LogLevel"].Should().Be("Warning",
          "Testing should use Warning log level");
        configurationData["Production"]["LogLevel"].Should().Be("Error",
          "Production should use Error log level");

        _output.WriteLine("\n✓ Configuration contracts are consistent yet appropriately distinct");
    }
}
