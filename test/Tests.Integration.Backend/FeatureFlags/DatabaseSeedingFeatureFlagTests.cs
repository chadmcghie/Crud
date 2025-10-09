using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for Database Seeding feature flag
/// Validates that database seeding can be toggled on/off per environment
/// </summary>
/// <remarks>
/// This is a Permission Toggle (long-lived) for environment-based control of database seeding.
/// Production: Disabled, Development/Testing: Enabled
/// </remarks>
public class DatabaseSeedingFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public DatabaseSeedingFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that DatabaseSeeding feature flag can be checked at runtime
    /// </summary>
    [Fact]
    public async Task DatabaseSeeding_FeatureFlag_CanBeChecked()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.DatabaseSeeding);

        // Assert
        // In Testing environment, seeding should be enabled
        Assert.True(isEnabled, "DatabaseSeeding should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that DatabaseSeeding flag has correct defaults per environment
    /// </summary>
    [Theory]
    [InlineData("Development", true)]  // Enable seeding in development
    [InlineData("Testing", true)]      // Enable seeding for tests
    [InlineData("Production", false)]  // Disable seeding in production (security)
    public void DatabaseSeeding_ShouldHaveCorrectDefaults_PerEnvironment(string environment, bool expectedState)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var seedingEnabled = configuration.GetValue<bool>("FeatureManagement:DatabaseSeeding");

        // Assert
        Assert.Equal(expectedState, seedingEnabled);
    }

    /// <summary>
    /// Validates that application starts correctly regardless of seeding flag state
    /// </summary>
    [Fact]
    public async Task Application_ShouldStart_RegardlessOfSeedingFlag()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Validates that database seeding is a security concern in production
    /// </summary>
    [Fact]
    public void DatabaseSeeding_ShouldBeDisabled_InProduction()
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment("Production");

        // Act
        var seedingEnabled = configuration.GetValue<bool>("FeatureManagement:DatabaseSeeding");

        // Assert
        Assert.False(seedingEnabled, "Database seeding must be disabled in Production for security");
    }

    /// <summary>
    /// Validates that database seeding is enabled for development and testing
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void DatabaseSeeding_ShouldBeEnabled_InDevelopmentAndTesting(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var seedingEnabled = configuration.GetValue<bool>("FeatureManagement:DatabaseSeeding");

        // Assert
        Assert.True(seedingEnabled, $"Database seeding should be enabled in {environment} environment");
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
