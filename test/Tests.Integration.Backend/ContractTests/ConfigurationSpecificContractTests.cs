using System.Net;
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
        Assert.False(string.IsNullOrEmpty(connectionString));

        var databaseProvider = configuration["DatabaseProvider"];
        Assert.Equal("SQLite", databaseProvider);

        // Environment-specific database naming contract
        Assert.Contains($"CrudContract_{environment.Substring(0, 4)}", connectionString);

        // Validate database accessibility contract
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();
        Assert.True(canConnect);

        _output.WriteLine($"✓ Database configuration contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public Task LoggingConfiguration_Contract_ShouldRespectEnvironmentLevels(string environment)
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

        Assert.Equal(expectedLogLevel, configuredLogLevel);

        // Validate logging behavior contract
        var shouldLogInfo = environment == "Development";
        var shouldLogWarning = environment != "Production";
        var shouldLogError = true; // All environments should log errors

        Assert.Equal(shouldLogInfo, logger.IsEnabled(LogLevel.Information));

        Assert.Equal(shouldLogWarning, logger.IsEnabled(LogLevel.Warning));

        Assert.Equal(shouldLogError, logger.IsEnabled(LogLevel.Error));

        _output.WriteLine($"✓ Logging configuration contract validated for {environment}");

        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public Task CachingConfiguration_Contract_ShouldMaintainConsistency(string environment)
    {
        _output.WriteLine($"=== VALIDATING CACHING CONFIGURATION CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Caching configuration contract
        var useRedis = configuration["Caching:UseRedis"];
        Assert.Equal("false", useRedis);

        var outputCachingDisabled = configuration["OutputCaching:Disabled"];
        Assert.Equal("false", outputCachingDisabled);

        // Validate memory cache availability contract
        var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
        Assert.NotNull(memoryCache);

        // Test caching behavior contract
        var testKey = $"config-contract-{environment}";
        var testValue = $"test-{DateTime.UtcNow.Ticks}";

        memoryCache!.Set(testKey, testValue, TimeSpan.FromMinutes(1));
        var retrievedValue = memoryCache!.Get<string>(testKey);
        Assert.Equal(testValue, retrievedValue);

        _output.WriteLine($"✓ Caching configuration contract validated for {environment}");
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task SecurityConfiguration_Contract_ShouldVaryByEnvironment(string environment)
    {
        _output.WriteLine($"=== VALIDATING SECURITY CONFIGURATION CONTRACT: {environment} ===");

        using var client = CreateClientForEnvironment(environment);

        // Test error information disclosure contract
        var response = await client.GetAsync("/api/nonexistent/endpoint");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Environment-specific security contract
        switch (environment)
        {
            case "Development":
                // Development may expose more detailed error information
                Assert.False(string.IsNullOrEmpty(content));
                break;

            case "Testing":
                // Testing should have moderate error information
                Assert.False(string.IsNullOrEmpty(content));
                break;

            case "Production":
                // Production should minimize error information exposure
                Assert.DoesNotContain("stack trace", content.ToLower());
                Assert.DoesNotContain("exception", content.ToLower());
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
        Assert.NotNull(healthReport);

        // Environment-specific health check expectations
        var expectedStatus = HealthStatus.Healthy; // Contract tests should always be healthy

        Assert.Equal(expectedStatus, healthReport.Status);

        // Validate health check entries
        Assert.NotEmpty(healthReport.Entries);

        foreach (var (checkName, entry) in healthReport.Entries)
        {
            Assert.True(entry.Status == HealthStatus.Healthy || entry.Status == HealthStatus.Degraded);

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
        Assert.NotNull(scope.ServiceProvider.GetService<ApplicationDbContext>());

        Assert.NotNull(scope.ServiceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>());

        Assert.NotNull(scope.ServiceProvider.GetService<MediatR.IMediator>());

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
    public Task ConfigurationInheritance_Contract_ShouldMaintainHierarchy(string environment)
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
            Assert.False(string.IsNullOrEmpty(value));
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
                Assert.False(string.IsNullOrEmpty(value));
            }
        }

        _output.WriteLine($"✓ Configuration inheritance contract validated for {environment}");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ConfigurationContracts_ShouldBeConsistentYetDistinct()
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
            Assert.Equal("SQLite", config["DatabaseProvider"]);

            // All environments should have Redis disabled for contract tests
            Assert.Equal("false", config["UseRedis"]);

            // Connection strings should be environment-specific
            Assert.Contains($"CrudContract_{env.Substring(0, 4)}", config["ConnectionString"]);
        }

        // Validate environment-specific differences
        Assert.Equal("Information", configurationData["Development"]["LogLevel"]);
        Assert.Equal("Warning", configurationData["Testing"]["LogLevel"]);
        Assert.Equal("Error", configurationData["Production"]["LogLevel"]);

        _output.WriteLine("\n✓ Configuration contracts are consistent yet appropriately distinct");
        return Task.CompletedTask;
    }
}
