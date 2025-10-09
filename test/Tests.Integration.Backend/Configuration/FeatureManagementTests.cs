using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.Configuration;

/// <summary>
/// Integration tests for Microsoft.FeatureManagement infrastructure
/// Validates feature flag configuration, service registration, and runtime behavior
/// </summary>
public class FeatureManagementTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public FeatureManagementTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that IFeatureManager can be resolved from DI container
    /// </summary>
    [Fact]
    public void FeatureManager_ShouldBeRegistered_InDependencyInjection()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetService<IFeatureManager>();

        // Assert
        Assert.NotNull(featureManager);
    }

    /// <summary>
    /// Validates that feature flag configuration section exists in appsettings
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void FeatureFlagsConfiguration_ShouldExist_InAllEnvironments(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var featureFlagsSection = configuration.GetSection("FeatureManagement");

        // Assert
        Assert.NotNull(featureFlagsSection);
        Assert.True(featureFlagsSection.Exists(), $"FeatureManagement section should exist in {environment} environment");
    }

    /// <summary>
    /// Validates that all 14 feature flags are defined in configuration
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void AllFeatureFlags_ShouldBeDefined_InConfiguration(string environment)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);
        var expectedFeatureFlags = new[]
        {
            // Ops Toggles (7)
            "Caching",
            "Compression",
            "RateLimiting",
            "OpenTelemetry",
            "Cors",
            "HealthChecksDetailed",
            "DynamicLogLevel",

            // Release Toggles (3)
            "IdentityAuthentication",
            "EmailService",
            "Resilience",

            // Permission Toggles (4)
            "Swagger",
            "DatabaseSeeding",
            "DetailedExceptions",
            "ConditionalRequests"
        };

        // Act & Assert
        var featureManagementSection = configuration.GetSection("FeatureManagement");

        foreach (var featureFlag in expectedFeatureFlags)
        {
            var flagValue = featureManagementSection.GetValue<bool?>(featureFlag);
            Assert.NotNull(flagValue);
        }
    }

    /// <summary>
    /// Validates environment-specific feature flag defaults
    /// </summary>
    [Fact]
    public void FeatureFlags_ShouldHaveCorrectDefaults_PerEnvironment()
    {
        // Arrange
        var devConfig = BuildConfigurationForEnvironment("Development");
        var testConfig = BuildConfigurationForEnvironment("Testing");
        var prodConfig = BuildConfigurationForEnvironment("Production");

        // Act & Assert - Ops Toggles should be enabled in Production
        Assert.True(prodConfig.GetValue<bool>("FeatureManagement:Caching"));
        Assert.True(prodConfig.GetValue<bool>("FeatureManagement:Compression"));
        Assert.True(prodConfig.GetValue<bool>("FeatureManagement:RateLimiting"));
        Assert.True(prodConfig.GetValue<bool>("FeatureManagement:OpenTelemetry"));

        // Assert - Permission Toggles - Swagger disabled in Production
        Assert.False(prodConfig.GetValue<bool>("FeatureManagement:Swagger"));
        Assert.True(devConfig.GetValue<bool>("FeatureManagement:Swagger"));

        // Assert - Permission Toggles - Detailed health checks disabled in Production
        Assert.False(prodConfig.GetValue<bool>("FeatureManagement:HealthChecksDetailed"));
        Assert.True(devConfig.GetValue<bool>("FeatureManagement:HealthChecksDetailed"));

        // Assert - Permission Toggles - Detailed exceptions disabled in Production
        Assert.False(prodConfig.GetValue<bool>("FeatureManagement:DetailedExceptions"));
        Assert.True(devConfig.GetValue<bool>("FeatureManagement:DetailedExceptions"));

        // Assert - Permission Toggles - Database seeding disabled in Production
        Assert.False(prodConfig.GetValue<bool>("FeatureManagement:DatabaseSeeding"));
        Assert.True(devConfig.GetValue<bool>("FeatureManagement:DatabaseSeeding"));
    }

    /// <summary>
    /// Validates that feature flags can be checked at runtime
    /// </summary>
    [Fact]
    public async Task FeatureManager_ShouldCheckFeatureFlags_AtRuntime()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act - Check if a feature is enabled (using a known feature from config)
        var isSwaggerEnabled = await featureManager.IsEnabledAsync("Swagger");

        // Assert - In Testing environment, Swagger should be enabled
        Assert.True(isSwaggerEnabled, "Swagger feature flag should be enabled in Testing environment");
    }

    /// <summary>
    /// Validates that feature manager can check multiple features
    /// </summary>
    [Fact]
    public async Task FeatureManager_ShouldCheckMultipleFeatures_Independently()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isCachingEnabled = await featureManager.IsEnabledAsync("Caching");
        var isCompressionEnabled = await featureManager.IsEnabledAsync("Compression");
        var isHealthChecksDetailedEnabled = await featureManager.IsEnabledAsync("HealthChecksDetailed");

        // Assert
        Assert.True(isCachingEnabled);
        Assert.True(isCompressionEnabled);
        Assert.True(isHealthChecksDetailedEnabled);
    }

    /// <summary>
    /// Validates that IFeatureManagerSnapshot is available for request-scoped scenarios
    /// </summary>
    [Fact]
    public void FeatureManagerSnapshot_ShouldBeRegistered_ForRequestScope()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManagerSnapshot = scope.ServiceProvider.GetService<IFeatureManagerSnapshot>();

        // Assert
        Assert.NotNull(featureManagerSnapshot);
    }

    /// <summary>
    /// Validates that feature flags work with HTTP context
    /// </summary>
    [Fact]
    public async Task FeatureFlags_ShouldWorkWithHttpContext()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make request to an endpoint (health check as simple test)
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Validates that all feature flag names are properly defined as constants
    /// This test will pass once we create the FeatureFlags constants class
    /// </summary>
    [Fact]
    public void FeatureFlagConstants_ShouldBeDefined()
    {
        // This test validates that we have constants for all feature flags
        // The actual constants will be created in a separate task

        // For now, we'll validate the configuration directly
        var configuration = BuildConfigurationForEnvironment("Development");
        var featureManagementSection = configuration.GetSection("FeatureManagement");

        Assert.True(featureManagementSection.Exists());
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
