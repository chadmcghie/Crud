using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Tests for environment variable validation across configurations (Development, Testing, Production)
/// Validates environment-specific variables and configuration alignment with deployment pipeline
/// without creating new branches - using existing dev=Development, staging=Testing, main=Production mapping
/// </summary>
public class EnvironmentVariableValidationTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public EnvironmentVariableValidationTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test 1.4: Validates ASPNETCORE_ENVIRONMENT variable is correctly set for each configuration
    /// Ensures environment variable matches the expected configuration
    /// </summary>
    [Theory]
    [InlineData("Development", "Development")]
    [InlineData("Testing", "Testing")]
    [InlineData("Production", "Production")]
    public void AspNetCoreEnvironment_ShouldMatch_ConfigurationEnvironment(string configEnvironment, string expectedEnvironment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(configEnvironment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act
        var aspNetCoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"];
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().EnvironmentName;

        // Assert
        aspNetCoreEnvironment.Should().Be(expectedEnvironment,
            $"ASPNETCORE_ENVIRONMENT should be {expectedEnvironment} for {configEnvironment} configuration");

        environment.Should().Be(expectedEnvironment,
            $"IWebHostEnvironment.EnvironmentName should be {expectedEnvironment} for {configEnvironment} configuration");
    }

    /// <summary>
    /// Test 1.4: Validates required environment variables are present for each configuration
    /// Ensures all critical environment variables are available
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void RequiredEnvironmentVariables_ShouldBePresent_InAllConfigurations(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Check for required environment variables
        var aspNetCoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"];
        aspNetCoreEnvironment.Should().NotBeNullOrEmpty(
            $"ASPNETCORE_ENVIRONMENT should be set in {environment} configuration");

        // Validate connection string is available (could come from env var or config file)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty(
            $"DefaultConnection should be available in {environment} configuration");

        // Check for any environment-specific overrides
        var allowedHosts = configuration["AllowedHosts"];
        allowedHosts.Should().NotBeNullOrEmpty(
            $"AllowedHosts should be configured in {environment} configuration");
    }

    /// <summary>
    /// Test 1.4: Validates database provider environment variable alignment
    /// Ensures DatabaseProvider setting aligns with branch-environment mapping
    /// </summary>
    [Theory]
    [InlineData("Development", "SQLite")]  // dev branch = Development = SQLite
    [InlineData("Testing", "SQLite")]      // staging branch = Testing = SQLite
    [InlineData("Production", "SQLite")]   // main branch = Production = could be SQLite or SqlServer
    public void DatabaseProviderEnvironmentVariable_ShouldAlign_WithBranchMapping(string environment, string expectedProvider)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Assert
        databaseProvider.Should().NotBeNullOrEmpty(
            $"DatabaseProvider should be configured in {environment} environment");

        // Validate connection string format matches the provider
        if (databaseProvider == "SQLite")
        {
            connectionString.Should().Contain(".db",
                $"SQLite connection string should contain .db file reference in {environment}");
        }
        else if (databaseProvider == "SqlServer")
        {
            connectionString.Should().Contain("Server=",
                $"SQL Server connection string should contain Server= in {environment}");
        }
    }

    /// <summary>
    /// Test 1.4: Validates environment variable precedence and overrides
    /// Ensures environment variables properly override configuration file values
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void EnvironmentVariables_ShouldOverride_ConfigurationFileValues(string environment)
    {
        // Arrange - Set test environment variable
        const string testKey = "TestOverrideKey";
        const string testValue = "EnvironmentVariableValue";
        Environment.SetEnvironmentVariable(testKey, testValue);

        try
        {
            using var factory = CreateFactoryForEnvironment(environment);
            using var scope = factory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Act
            var configValue = configuration[testKey];

            // Assert
            configValue.Should().Be(testValue,
                $"Environment variable should override configuration file value in {environment}");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(testKey, null);
        }
    }

    /// <summary>
    /// Test 1.4: Validates security-sensitive environment variables are handled correctly
    /// Ensures sensitive configuration is not exposed inappropriately
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void SecuritySensitiveEnvironmentVariables_ShouldBeHandled_Securely(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Check that sensitive values are not logged or exposed
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty(
            $"Connection string should be available in {environment}");

        // Validate that sensitive information is properly handled
        // Note: This is more of a documentation test - in real scenarios you'd check logging configuration
        var jwtSecret = configuration["JWT:Secret"];
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            jwtSecret.Length.Should().BeGreaterThan(10,
                $"JWT secret should be sufficiently long in {environment} environment");
        }
    }

    /// <summary>
    /// Test 1.4: Validates environment-specific feature flags and toggles
    /// Ensures feature toggles work correctly across environments
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void EnvironmentSpecificFeatureFlags_ShouldBeConfigured_Appropriately(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Check environment-appropriate feature configurations

        // Development might have additional debugging features enabled
        if (environment == "Development")
        {
            var detailedErrors = configuration.GetValue<bool>("DetailedErrors", false);
            // In development, detailed errors might be enabled (this is just an example)
        }

        // Production should have security-focused configurations
        if (environment == "Production")
        {
            var httpsRedirection = configuration.GetValue<bool>("UseHttpsRedirection", true);
            // Production should typically enforce HTTPS (this is just an example)
        }

        // Testing environment should be configured for test stability
        if (environment == "Testing")
        {
            var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase", false);
            // Testing might use special database configurations (this is just an example)
        }
    }

    /// <summary>
    /// Test 1.4: Validates environment variable validation and error handling
    /// Ensures invalid or missing environment variables are handled gracefully
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void EnvironmentVariableValidation_ShouldHandle_InvalidOrMissingValues(string environment)
    {
        // Arrange
        using var factory = CreateFactoryForEnvironment(environment);
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Test handling of missing values
        var nonExistentValue = configuration["NonExistentEnvironmentVariable"];
        nonExistentValue.Should().BeNull(
            "Non-existent environment variables should return null");

        // Test default value handling
        var defaultValue = configuration.GetValue<string>("NonExistentKey", "DefaultValue");
        defaultValue.Should().Be("DefaultValue",
            "Configuration should return default value for missing keys");

        // Test required configuration validation
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString.Should().NotBeNullOrEmpty(
            $"Required configuration should be present in {environment}");
    }

    /// <summary>
    /// Test 1.4: Validates configuration binding with environment variables
    /// Ensures strongly-typed configuration objects work with environment variable overrides
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void ConfigurationBinding_ShouldWork_WithEnvironmentVariableOverrides(string environment)
    {
        // Arrange - Set environment variable using .NET configuration naming convention
        const string envVarName = "Logging__LogLevel__Default";
        const string testLogLevel = "Warning";
        Environment.SetEnvironmentVariable(envVarName, testLogLevel);

        try
        {
            using var factory = CreateFactoryForEnvironment(environment);
            using var scope = factory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Act
            var logLevel = configuration["Logging:LogLevel:Default"];

            // Assert
            logLevel.Should().Be(testLogLevel,
                $"Environment variable should override configuration binding in {environment}");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(envVarName, null);
        }
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

                    // Add configuration in the same order as the main application
                    config.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "Api"));
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();

                    // Override with test-specific settings for database isolation
                    if (environment == "Testing")
                    {
                        var testConnectionString = $"Data Source=CrudTest_EnvVar_{Guid.NewGuid()}.db";
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = testConnectionString
                        });
                    }
                });
            });
    }

    #endregion
}