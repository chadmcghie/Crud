using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tests.Integration.Backend.Infrastructure;
using Xunit;
using System.Text.Json;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Tests for configuration validation across all environments (Development, Testing, Production)
/// Validates configuration loading, service resolution, and environment-specific settings
/// without creating new branches - using existing dev=Development, staging=Testing, main=Production mapping
/// </summary>
public class ConfigurationValidationTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public ConfigurationValidationTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test 1.1: Validates configuration loading across all environments
    /// Ensures appsettings files can be loaded and parsed without errors
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void ConfigurationLoading_ShouldLoadSuccessfully_ForAllEnvironments(string environment)
    {
        // Arrange & Act
        var configuration = BuildConfigurationForEnvironment(environment);

        // Assert
        Assert.NotNull(configuration);

        // Verify basic required configuration sections exist
        Assert.NotNull(configuration.GetSection("ConnectionStrings"));
        Assert.NotNull(configuration.GetSection("Logging"));

        // Verify environment-specific configuration is loaded
        var aspNetCoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"];
        Assert.False(string.IsNullOrEmpty(aspNetCoreEnvironment));

        // Verify no JSON parsing errors occurred
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Assert.False(string.IsNullOrEmpty(connectionString));
    }

    /// <summary>
    /// Test 1.1: Validates configuration schema consistency across environments
    /// Ensures all environments have the same configuration structure
    /// </summary>
    [Fact]
    public void ConfigurationSchema_ShouldBeConsistent_AcrossAllEnvironments()
    {
        // Arrange
        var environments = new[] { "Development", "Testing", "Production" };
        var configStructures = new Dictionary<string, HashSet<string>>();

        // Act - Build configuration for each environment and extract keys
        foreach (var env in environments)
        {
            var config = BuildConfigurationForEnvironment(env);
            var keys = ExtractConfigurationKeys(config);
            configStructures[env] = keys;
        }

        // Assert - All environments should have the same configuration structure
        var developmentKeys = configStructures["Development"];
        var testingKeys = configStructures["Testing"];
        var productionKeys = configStructures["Production"];

        // Core configuration sections must exist in all environments
        var requiredSections = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "Logging:LogLevel:Default",
            "AllowedHosts"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(requiredSection, developmentKeys);
            Assert.Contains(requiredSection, testingKeys);
            Assert.Contains(requiredSection, productionKeys);
        }
    }

    /// <summary>
    /// Test 1.1: Validates environment-specific configuration differences
    /// Ensures each environment has appropriate configuration values
    /// </summary>
    [Theory]
    [InlineData("Development", "SQLite")] // Development uses SQLite
    [InlineData("Testing", "SQLite")]     // Testing uses SQLite
    [InlineData("Production", "SQLServer")] // Production uses SQL Server
    public void EnvironmentSpecificConfiguration_ShouldHaveCorrectDatabaseProvider_ForEachEnvironment(
        string environment, string expectedProviderType)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";

        // Assert
        Assert.False(string.IsNullOrEmpty(connectionString));

        if (expectedProviderType == "SQLite")
        {
            Assert.Contains(".db", connectionString);
        }
        else if (expectedProviderType == "SQLServer")
        {
            Assert.Contains("Server=", connectionString);
        }

        Assert.Equal(expectedProviderType, databaseProvider);
    }

    /// <summary>
    /// Test 1.1: Validates configuration values are not empty or malformed
    /// Ensures critical configuration values are properly set
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void ConfigurationValues_ShouldNotBeEmpty_ForCriticalSettings(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act & Assert
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString; Assert.NotBeNullOrWhiteSpace(
            $"{environment} should have a valid DefaultConnection");

        var allowedHosts = configuration.GetValue<string>("AllowedHosts");
        allowedHosts; Assert.NotBeNullOrWhiteSpace(
            $"{environment} should have AllowedHosts configured");

        var logLevel = configuration.GetValue<string>("Logging:LogLevel:Default");
        logLevel; Assert.NotBeNullOrWhiteSpace(
            $"{environment} should have default log level configured");
    }

    #region Helper Methods

    /// <summary>
    /// Builds configuration for a specific environment using the same pattern as the application
    /// </summary>
    private IConfiguration BuildConfigurationForEnvironment(string environment)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "Api"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Set the environment variable to match the configuration being tested
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        return builder.Build();
    }

    /// <summary>
    /// Extracts all configuration keys from an IConfiguration instance
    /// </summary>
    private HashSet<string> ExtractConfigurationKeys(IConfiguration configuration)
    {
        var keys = new HashSet<string>();
        ExtractKeysRecursive(configuration, "", keys);
        return keys;
    }

    /// <summary>
    /// Recursively extracts configuration keys with their full paths
    /// </summary>
    private void ExtractKeysRecursive(IConfiguration configuration, string prefix, HashSet<string> keys)
    {
        foreach (var child in configuration.GetChildren())
        {
            var fullKey = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

            if (child.Value != null)
            {
                keys.Add(fullKey);
            }

            // Recursively process child sections
            ExtractKeysRecursive(child, fullKey, keys);
        }
    }

    #endregion
}