using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for Serilog Dynamic Log Level feature flag
/// Validates that log level configuration respects the DynamicLogLevel feature flag
/// </summary>
public class SerilogFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public SerilogFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that DynamicLogLevel feature flag can be checked at runtime
    /// </summary>
    [Fact]
    public async Task DynamicLogLevel_FeatureFlag_CanBeChecked()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.DynamicLogLevel);

        // Assert
        // In Testing environment, DynamicLogLevel should be configurable (currently false in appsettings.Testing.json)
        Assert.False(isEnabled, "DynamicLogLevel should be disabled in Testing environment by default");
    }

    /// <summary>
    /// Validates that Serilog is always configured regardless of feature flag
    /// The flag only controls dynamic log level changes, not Serilog itself
    /// </summary>
    [Fact]
    public void Serilog_ShouldAlwaysBeConfigured_RegardlessOfFeatureFlag()
    {
        // Arrange & Act
        var client = _factory.CreateClient();

        // Assert - If Serilog wasn't configured, the app wouldn't start
        Assert.NotNull(client);
    }

    /// <summary>
    /// Validates that logging configuration exists in appsettings
    /// </summary>
    [Fact]
    public void SerilogConfiguration_ShouldExist_InAppsettings()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Act
        var serilogSection = configuration.GetSection("Serilog");

        // Assert
        Assert.NotNull(serilogSection);
        Assert.True(serilogSection.Exists(), "Serilog configuration section should exist");
    }

    /// <summary>
    /// Validates that DynamicLogLevel feature flag is properly configured
    /// </summary>
    [Theory]
    [InlineData("Development", false)] // Development: Debug level (static)
    [InlineData("Testing", false)]     // Testing: Warning level (static)
    [InlineData("Production", false)]  // Production: Information level (static)
    public void DynamicLogLevel_ShouldHaveCorrectDefaults_PerEnvironment(string environment, bool expectedState)
    {
        // Note: For now, DynamicLogLevel is disabled in all environments
        // This is a placeholder for future dynamic log level functionality
        // When implemented, Production might enable this for runtime adjustment

        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var dynamicLogLevelEnabled = configuration.GetValue<bool>("FeatureManagement:DynamicLogLevel");

        // Assert
        Assert.Equal(expectedState, dynamicLogLevelEnabled);
    }

    /// <summary>
    /// Validates that minimum log level is configured per environment
    /// </summary>
    [Theory]
    [InlineData("Development")] // Debug level
    [InlineData("Testing")]     // Warning level
    [InlineData("Production")]  // Information level
    public void Serilog_MinimumLevel_ShouldBeConfigured_PerEnvironment(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var minimumLevel = configuration.GetValue<string>("Serilog:MinimumLevel:Default");

        // Assert
        Assert.NotNull(minimumLevel);
        Assert.NotEmpty(minimumLevel);
    }

    #region Helper Methods

    /// <summary>
    /// Builds configuration for a specific environment
    /// </summary>
    private IConfiguration BuildConfigurationForEnvironment(string environment)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var repoRoot = currentDirectory;

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
