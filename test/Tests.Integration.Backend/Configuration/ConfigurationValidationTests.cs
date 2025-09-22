using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Tests for configuration validation across environments (Development, Testing, Production)
/// Validates configuration loading and environment-specific settings work correctly
/// </summary>
public class ConfigurationValidationTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public ConfigurationValidationTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that basic configuration can be loaded for each environment
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void ConfigurationLoading_ShouldLoadSuccessfully_ForAllEnvironments(string environment)
    {
        // Arrange & Act
        var configuration = BuildConfigurationForEnvironment(environment);

        // Assert - Basic configuration structure
        Assert.NotNull(configuration);

        // Verify database connection string exists
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Assert.NotNull(connectionString);
        Assert.NotEmpty(connectionString);

        // Verify database provider is configured
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
        Assert.Equal("SQLite", databaseProvider);

        // Verify required sections exist
        Assert.NotNull(configuration.GetSection("Serilog"));
        Assert.NotNull(configuration.GetSection("AllowedHosts"));
    }

    /// <summary>
    /// Validates environment-specific configuration differences
    /// </summary>
    [Fact]
    public void EnvironmentSpecificConfiguration_ShouldHaveDifferentValues()
    {
        // Arrange
        var devConfig = BuildConfigurationForEnvironment("Development");
        var testConfig = BuildConfigurationForEnvironment("Testing");
        var prodConfig = BuildConfigurationForEnvironment("Production");

        // Assert - Connection strings should be different
        var devConnection = devConfig.GetConnectionString("DefaultConnection");
        var testConnection = testConfig.GetConnectionString("DefaultConnection");

        Assert.NotEqual(devConnection, testConnection);
        Assert.Contains("CrudApp.db", devConnection);
        Assert.Contains("TestDatabase.db", testConnection);

        // Testing environment should have database reset enabled
        var testingResetEnabled = testConfig.GetValue<bool>("Testing:EnableDatabaseReset");
        var devResetEnabled = devConfig.GetValue<bool>("Testing:EnableDatabaseReset");

        Assert.True(testingResetEnabled);
        Assert.False(devResetEnabled);
    }

    /// <summary>
    /// Validates JWT configuration is present and properly configured
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void JwtConfiguration_ShouldBeValid_InAllEnvironments(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act & Assert
        var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer");
        var jwtAudience = configuration.GetValue<string>("Jwt:Audience");

        Assert.NotNull(jwtIssuer);
        Assert.NotNull(jwtAudience);
        Assert.NotEmpty(jwtIssuer);
        Assert.NotEmpty(jwtAudience);

        // Testing environment should have a test-specific JWT secret
        if (environment == "Testing")
        {
            var jwtSecret = configuration.GetValue<string>("Jwt:Secret");
            Assert.NotNull(jwtSecret);
            Assert.Contains("Test", jwtSecret);
        }
    }

    /// <summary>
    /// Validates caching configuration is properly set up
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void CachingConfiguration_ShouldBeConfigured_InAllEnvironments(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act & Assert
        var useRedis = configuration.GetValue<bool>("Caching:UseRedis");
        var useLazyCache = configuration.GetValue<bool>("Caching:UseLazyCache");
        var useOutputCaching = configuration.GetValue<bool>("Caching:UseOutputCaching");

        // These are the default values from appsettings.json
        Assert.False(useRedis);
        Assert.True(useLazyCache);
        Assert.True(useOutputCaching);

        // Verify cache duration settings exist
        var defaultExpiration = configuration.GetValue<int>("Caching:DefaultExpirationMinutes");
        Assert.True(defaultExpiration > 0);
    }

    #region Helper Methods

    /// <summary>
    /// Builds configuration for a specific environment using the same pattern as the application
    /// </summary>
    private IConfiguration BuildConfigurationForEnvironment(string environment)
    {
        // Navigate to the src/Api directory from the test project
        var currentDirectory = Directory.GetCurrentDirectory();
        var repoRoot = currentDirectory;

        // Find the repo root by looking for the .git directory or src folder
        while (!Directory.Exists(Path.Combine(repoRoot, "src")) && Directory.GetParent(repoRoot) != null)
        {
            repoRoot = Directory.GetParent(repoRoot)!.FullName;
        }

        var apiConfigPath = Path.Combine(repoRoot, "src", "Api");

        var builder = new ConfigurationBuilder()
            .SetBasePath(apiConfigPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    #endregion
}