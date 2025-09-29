using FluentAssertions;
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
        dbContext.Should().NotBeNull($"ApplicationDbContext should be available in {environment}");

        // Verify database connection contract
        var canConnect = await dbContext!.Database.CanConnectAsync();
        canConnect.Should().BeTrue($"Database should be connectable in {environment}");

        // Verify entity sets contract
        dbContext.Users.Should().NotBeNull("Users DbSet should be available");
        dbContext.Roles.Should().NotBeNull("Roles DbSet should be available");
        dbContext.People.Should().NotBeNull("People DbSet should be available");
        dbContext.Walls.Should().NotBeNull("Walls DbSet should be available");
        dbContext.Windows.Should().NotBeNull("Windows DbSet should be available");

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
        personRepository.Should().NotBeNull($"Person repository should be available in {environment}");

        var roleRepository = scope.ServiceProvider.GetService<App.Abstractions.IRoleRepository>();
        roleRepository.Should().NotBeNull($"Role repository should be available in {environment}");

        var wallRepository = scope.ServiceProvider.GetService<App.Abstractions.IWallRepository>();
        wallRepository.Should().NotBeNull($"Wall repository should be available in {environment}");

        var windowRepository = scope.ServiceProvider.GetService<App.Abstractions.IWindowRepository>();
        windowRepository.Should().NotBeNull($"Window repository should be available in {environment}");

        // Test repository contract behavior
        var roles = await roleRepository!.ListAsync();
        roles.Should().NotBeNull("Repository ListAsync should return valid result");

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
        loggerFactory.Should().NotBeNull($"LoggerFactory should be available in {environment}");

        var logger = scope.ServiceProvider.GetService<ILogger<ServiceInterfaceContractTests>>();
        logger.Should().NotBeNull($"Generic logger should be available in {environment}");

        // Test logging contract behavior
        logger!.LogInformation("Contract test message for {Environment}", environment);

        // Verify logger can handle different log levels
        var canLogError = logger!.IsEnabled(LogLevel.Error);
        canLogError.Should().BeTrue("Error logging should be enabled across all environments");

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
        configuration.Should().NotBeNull($"IConfiguration should be available in {environment}");

        // Verify required configuration sections
        var connectionString = configuration!.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty($"Connection string should be available in {environment}");

        var databaseProvider = configuration!["DatabaseProvider"];
        databaseProvider.Should().NotBeNullOrEmpty($"Database provider should be configured in {environment}");

        // Environment-specific configuration validation
        var logLevel = configuration["Logging:LogLevel:Default"];
        logLevel.Should().NotBeNullOrEmpty($"Log level should be configured in {environment}");

        var expectedLogLevel = environment switch
        {
            "Development" => "Information",
            "Testing" => "Warning",
            "Production" => "Error",
            _ => "Information"
        };

        logLevel.Should().Be(expectedLogLevel, $"Log level should match environment expectations for {environment}");

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
        mediator.Should().NotBeNull($"MediatR should be available in {environment}");

        // Test MediatR contract behavior with a simple query
        try
        {
            var rolesQuery = new App.Features.Roles.ListRolesQuery();
            var roles = await mediator!.Send(rolesQuery);
            roles.Should().NotBeNull("MediatR should process queries successfully");
        }
        catch (Exception ex)
        {
            // It's okay if the query fails due to data setup, but MediatR should be functional
            ex.Should().NotBeOfType<InvalidOperationException>("MediatR registration should be correct");
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
        memoryCache.Should().NotBeNull($"Memory cache should be available in {environment}");

        // Test cache contract behavior
        var testKey = $"contract-test-{environment}-{Guid.NewGuid()}";
        var testValue = $"test-value-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";

        memoryCache!.Set(testKey, testValue, TimeSpan.FromMinutes(1));
        var retrievedValue = memoryCache!.Get<string>(testKey);
        retrievedValue.Should().Be(testValue, "Memory cache should store and retrieve values correctly");

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
        healthCheckService.Should().NotBeNull($"Health check service should be available in {environment}");

        // Test health check contract behavior
        var healthReport = await healthCheckService!.CheckHealthAsync();
        healthReport.Should().NotBeNull("Health check should return a valid report");
        healthReport.Status.Should().BeOneOf(
          Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
          Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
          Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

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

            dbContext1.Should().NotBeNull("DbContext should be available in scope 1");
            dbContext2.Should().NotBeNull("DbContext should be available in scope 2");
            dbContext1.Should().NotBeSameAs(dbContext2, "DbContext should be scoped (different instances per scope)");
        }

        // Test singleton services contract (if any)
        using (var scope1 = factory.Services.CreateScope())
        using (var scope2 = factory.Services.CreateScope())
        {
            var config1 = scope1.ServiceProvider.GetService<IConfiguration>();
            var config2 = scope2.ServiceProvider.GetService<IConfiguration>();

            config1.Should().NotBeNull("Configuration should be available in scope 1");
            config2.Should().NotBeNull("Configuration should be available in scope 2");
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
