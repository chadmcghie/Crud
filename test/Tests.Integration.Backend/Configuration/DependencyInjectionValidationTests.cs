using App.Abstractions;
using App.Interfaces;
using Domain.Interfaces;
using Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Tests for dependency injection resolution validation across environments
/// Validates that all required services can be resolved for Development, Testing, Production configurations
/// without creating new branches - using existing dev=Development, staging=Testing, main=Production mapping
/// </summary>
public class DependencyInjectionValidationTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public DependencyInjectionValidationTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test 1.2: Validates core application services can be resolved in all environments
    /// Ensures dependency injection configuration is consistent across Development, Testing, Production
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void CoreApplicationServices_ShouldResolveSuccessfully_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - Core Application Services
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull($"IMediator should be registered in {environment}");

        var logger = serviceProvider.GetService<ILogger<DependencyInjectionValidationTests>>();
        logger.Should().NotBeNull($"ILogger should be registered in {environment}");

        var configuration = serviceProvider.GetService<IConfiguration>();
        configuration.Should().NotBeNull($"IConfiguration should be registered in {environment}");
    }

    /// <summary>
    /// Test 1.2: Validates data access services resolve correctly for each environment
    /// Ensures Entity Framework and repository services work with environment-specific configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void DataAccessServices_ShouldResolveSuccessfully_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - Data Access Services
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        dbContext.Should().NotBeNull($"CrudDbContext should be registered in {environment}");

        // Verify DbContext is configured correctly for the environment
        var connectionString = dbContext!.Database.GetConnectionString();
        connectionString.Should().NotBeNullOrEmpty($"DbContext should have connection string in {environment}");

        // Validate repository pattern services (if using repository pattern)
        var peopleRepository = serviceProvider.GetService<Domain.Interfaces.IRepository<Domain.Entities.Person>>();
        peopleRepository.Should().NotBeNull($"IRepository<Person> should be registered in {environment}");
    }

    /// <summary>
    /// Test 1.2: Validates caching services resolve correctly for each environment
    /// Ensures caching infrastructure works with environment-specific configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void CachingServices_ShouldResolveSuccessfully_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - Caching Services
        var cacheService = serviceProvider.GetService<ICacheService>();
        cacheService.Should().NotBeNull($"ICacheService should be registered in {environment}");

        // Verify cache management services for admin functionality
        var cacheStatisticsService = serviceProvider.GetService<ICacheStatisticsService>();
        cacheStatisticsService.Should().NotBeNull($"ICacheStatisticsService should be registered in {environment}");

        var cacheManagementService = serviceProvider.GetService<ICacheManagementService>();
        cacheManagementService.Should().NotBeNull($"ICacheManagementService should be registered in {environment}");
    }

    /// <summary>
    /// Test 1.2: Validates authentication and authorization services resolve correctly
    /// Ensures security services work with environment-specific configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void AuthenticationServices_ShouldResolveSuccessfully_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - Authentication Services
        var passwordHasher = serviceProvider.GetService<IPasswordHasher>();
        passwordHasher.Should().NotBeNull($"IPasswordHasher should be registered in {environment}");

        var emailService = serviceProvider.GetService<IEmailService>();
        emailService.Should().NotBeNull($"IEmailService should be registered in {environment}");

        var userRepository = serviceProvider.GetService<IUserRepository>();
        userRepository.Should().NotBeNull($"IUserRepository should be registered in {environment}");
    }

    /// <summary>
    /// Test 1.2: Validates environment-specific service configurations
    /// Ensures services have correct configurations for each environment
    /// </summary>
    [Fact]
    public void EnvironmentSpecificServices_ShouldHaveCorrectConfiguration_ForEachEnvironment()
    {
        // Test Development Environment
        using (var devFactory = CreateFactoryForEnvironment("Development"))
        {
            using var scope = devFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connectionString = dbContext.Database.GetConnectionString();
            connectionString.Should().Contain("CrudAppDev.db",
                "Development should use development SQLite database");
        }

        // Test Testing Environment
        using (var testFactory = CreateFactoryForEnvironment("Testing"))
        {
            using var scope = testFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connectionString = dbContext.Database.GetConnectionString();
            connectionString.Should().Contain(".db",
                "Testing should use SQLite database");
        }

        // Test Production Environment
        using (var prodFactory = CreateFactoryForEnvironment("Production"))
        {
            using var scope = prodFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connectionString = dbContext.Database.GetConnectionString();

            // Production might use SQL Server or SQLite depending on configuration
            connectionString.Should().NotBeNullOrEmpty(
                "Production should have a valid connection string");
        }
    }

    /// <summary>
    /// Test 1.2: Validates service resolution performance across environments
    /// Ensures dependency injection container performs well in all configurations
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void ServiceResolution_ShouldPerformWell_InAllEnvironments(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act & Assert - Performance test for service resolution
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Resolve multiple services to test container performance
        var services = new object[]
        {
            serviceProvider.GetRequiredService<IMediator>(),
            serviceProvider.GetRequiredService<ApplicationDbContext>(),
            serviceProvider.GetRequiredService<ICacheService>(),
            serviceProvider.GetRequiredService<IPasswordHasher>(),
            serviceProvider.GetRequiredService<ILogger<DependencyInjectionValidationTests>>()
        };

        stopwatch.Stop();

        // Assert all services were resolved
        services.Should().AllSatisfy(service => service.Should().NotBeNull());

        // Assert performance is reasonable (should be very fast)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            $"Service resolution should be fast in {environment} environment");
    }

    #region Helper Methods

    /// <summary>
    /// Creates a WebApplicationFactory configured for a specific environment
    /// </summary>
    private WebApplicationFactory<Api.Program> CreateFactoryForEnvironment(string environment)
    {
        return new WebApplicationFactory<Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Clear existing configuration
                    config.Sources.Clear();

                    // CRITICAL FIX: Load appsettings files with proper path resolution
                    // Find the API project directory dynamically
                    var currentDirectory = Directory.GetCurrentDirectory();
                    var repoRoot = currentDirectory;
                    while (!Directory.Exists(Path.Combine(repoRoot, "src")) && Directory.GetParent(repoRoot) != null)
                    {
                        repoRoot = Directory.GetParent(repoRoot)!.FullName;
                    }
                    var apiConfigPath = Path.Combine(repoRoot, "src", "Api");

                    // Add configuration in the same order as the main application
                    config.SetBasePath(apiConfigPath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();

                    // Override with test-specific settings for database isolation
                    if (environment == "Testing")
                    {
                        var testConnectionString = $"Data Source=CrudTest_{Guid.NewGuid()}.db";
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = testConnectionString
                        });
                    }
                });

                builder.ConfigureServices(services =>
                {
                    // Configure test-specific overrides if needed
                    // For example, replace email service with test implementation
                });
            });
    }

    #endregion
}
