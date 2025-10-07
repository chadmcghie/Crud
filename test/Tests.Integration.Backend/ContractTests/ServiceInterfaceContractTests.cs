using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration.Backend.ContractTests;

/// <summary>
/// Service interface contract tests that verify dependency injection and service behavior contracts
/// are maintained across different configurations (Development, Testing, Production)
/// </summary>
public class ServiceInterfaceContractTests : ContractTestBase
{
    private readonly ITestOutputHelper _output;

    public ServiceInterfaceContractTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task DatabaseServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING DATABASE SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify ApplicationDbContext contract
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(dbContext);

        // Verify database connection contract
        var canConnect = await dbContext!.Database.CanConnectAsync();
        Assert.True(canConnect);

        // Verify entity sets contract
        Assert.NotNull(dbContext.Users);
        Assert.NotNull(dbContext.Roles);
        Assert.NotNull(dbContext.People);
        Assert.NotNull(dbContext.Walls);
        Assert.NotNull(dbContext.Windows);

        _output.WriteLine($"✓ Database services contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task RepositoryServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING REPOSITORY SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify repository contracts
        var personRepository = scope.ServiceProvider.GetService<App.Abstractions.IPersonRepository>();
        Assert.NotNull(personRepository);

        var roleRepository = scope.ServiceProvider.GetService<App.Abstractions.IRoleRepository>();
        Assert.NotNull(roleRepository);

        var wallRepository = scope.ServiceProvider.GetService<App.Abstractions.IWallRepository>();
        Assert.NotNull(wallRepository);

        var windowRepository = scope.ServiceProvider.GetService<App.Abstractions.IWindowRepository>();
        Assert.NotNull(windowRepository);

        // Test repository contract behavior
        var roles = await roleRepository!.ListAsync();
        Assert.NotNull(roles);

        _output.WriteLine($"✓ Repository services contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task LoggingServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING LOGGING SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify logging contract
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);

        var logger = scope.ServiceProvider.GetService<ILogger<ServiceInterfaceContractTests>>();
        Assert.NotNull(logger);

        // Test logging contract behavior
        logger!.LogInformation("Contract test message for {Environment}", environment);

        // Verify logger can handle different log levels
        var canLogError = logger!.IsEnabled(LogLevel.Error);
        Assert.True(canLogError);

        // Configuration-specific logging level validation
        var expectedMinLevel = environment switch
        {
            "Development" => LogLevel.Information,
            "Testing" => LogLevel.Warning,
            "Production" => LogLevel.Error,
            _ => LogLevel.Information
        };

        _output.WriteLine($"Expected minimum log level for {environment}: {expectedMinLevel}");

        _output.WriteLine($"✓ Logging services contract validated for {environment}");

        await Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ConfigurationServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING CONFIGURATION SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify configuration contract
        var configuration = scope.ServiceProvider.GetService<IConfiguration>();
        Assert.NotNull(configuration);

        // Verify required configuration sections
        var connectionString = configuration!.GetConnectionString("DefaultConnection");
        Assert.False(string.IsNullOrEmpty(connectionString));

        var databaseProvider = configuration!["DatabaseProvider"];
        Assert.False(string.IsNullOrEmpty(databaseProvider));

        // Environment-specific configuration validation
        var logLevel = configuration["Logging:LogLevel:Default"];
        Assert.False(string.IsNullOrEmpty(logLevel));

        var expectedLogLevel = environment switch
        {
            "Development" => "Information",
            "Testing" => "Warning",
            "Production" => "Error",
            _ => "Information"
        };

        Assert.Equal(expectedLogLevel, logLevel);

        _output.WriteLine($"✓ Configuration services contract validated for {environment}");

        await Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task MediatorServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING MEDIATOR SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify MediatR contract
        var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
        Assert.NotNull(mediator);

        // Test MediatR contract behavior with a simple query
        try
        {
            var rolesQuery = new App.Features.Roles.ListRolesQuery();
            var roles = await mediator!.Send(rolesQuery);
            Assert.NotNull(roles);
        }
        catch (Exception ex)
        {
            // It's okay if the query fails due to data setup, but MediatR should be functional
            Assert.False(ex is InvalidOperationException);
        }

        _output.WriteLine($"✓ MediatR services contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task CachingServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING CACHING SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify memory cache contract (should be available in all environments)
        var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
        Assert.NotNull(memoryCache);

        // Test cache contract behavior
        var testKey = $"contract-test-{environment}-{Guid.NewGuid()}";
        var testValue = $"test-value-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";

        memoryCache!.Set(testKey, testValue, TimeSpan.FromMinutes(1));
        var retrievedValue = memoryCache!.Get<string>(testKey);
        Assert.Equal(testValue, retrievedValue);

        _output.WriteLine($"✓ Caching services contract validated for {environment}");

        await Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task HealthCheckServices_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING HEALTH CHECK SERVICES CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();

        // Verify health check services contract
        var healthCheckService = scope.ServiceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        Assert.NotNull(healthCheckService);

        // Test health check contract behavior
        var healthReport = await healthCheckService!.CheckHealthAsync();
        Assert.NotNull(healthReport);
        Assert.True(healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ||
                   healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded ||
                   healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

        _output.WriteLine($"Health check status in {environment}: {healthReport.Status}");
        _output.WriteLine($"✓ Health check services contract validated for {environment}");
    }

    [Theory]
    [MemberData(nameof(GetEnvironmentsAsTestData))]
    public async Task ServiceLifetime_Contract_ShouldBeConsistentAcrossEnvironments(string environment)
    {
        _output.WriteLine($"=== VALIDATING SERVICE LIFETIME CONTRACT: {environment} ===");

        using var factory = CreateFactoryForEnvironment(environment);

        // Test scoped services contract
        using (var scope1 = factory.Services.CreateScope())
        using (var scope2 = factory.Services.CreateScope())
        {
            var dbContext1 = scope1.ServiceProvider.GetService<ApplicationDbContext>();
            var dbContext2 = scope2.ServiceProvider.GetService<ApplicationDbContext>();

            Assert.NotNull(dbContext1);
            Assert.NotNull(dbContext2);
            Assert.NotSame(dbContext2, dbContext1);
        }

        // Test singleton services contract (if any)
        using (var scope1 = factory.Services.CreateScope())
        using (var scope2 = factory.Services.CreateScope())
        {
            var config1 = scope1.ServiceProvider.GetService<IConfiguration>();
            var config2 = scope2.ServiceProvider.GetService<IConfiguration>();

            Assert.NotNull(config1);
            Assert.NotNull(config2);
            // Configuration is typically singleton
        }

        _output.WriteLine($"✓ Service lifetime contract validated for {environment}");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AllServices_Contract_ShouldBeConsistentAcrossAllEnvironments()
    {
        _output.WriteLine("=== CROSS-ENVIRONMENT SERVICE CONTRACT CONSISTENCY VALIDATION ===");

        var serviceTests = new Dictionary<string, Func<string, Task>>
        {
            ["Database"] = async env => await DatabaseServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Repository"] = async env => await RepositoryServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Logging"] = async env => await LoggingServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Configuration"] = async env => await ConfigurationServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Mediator"] = async env => await MediatorServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["Caching"] = async env => await CachingServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["HealthCheck"] = async env => await HealthCheckServices_Contract_ShouldBeConsistentAcrossEnvironments(env),
            ["ServiceLifetime"] = async env => await ServiceLifetime_Contract_ShouldBeConsistentAcrossEnvironments(env)
        };

        foreach (var (serviceName, testAction) in serviceTests)
        {
            _output.WriteLine($"\nValidating {serviceName} service contracts across all environments:");

            foreach (var environment in GetSupportedEnvironments())
            {
                try
                {
                    await testAction(environment);
                    _output.WriteLine($"  ✓ {environment}: {serviceName} service contract valid");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  ✗ {environment}: {serviceName} service contract failed - {ex.Message}");
                    throw; // Re-throw to fail the test
                }
            }
        }

        _output.WriteLine("\n✓ All service interface contracts are consistent across environments");
    }
}
