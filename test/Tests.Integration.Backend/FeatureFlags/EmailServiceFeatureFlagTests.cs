using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for Email Service feature flag
/// Validates that email service implementation can be toggled between Mock and Real providers
/// </summary>
/// <remarks>
/// This is a Release Toggle (short-lived) for gradual rollout of real email service.
/// Expected lifecycle: 1-2 weeks after real email service is stable, then remove toggle.
/// </remarks>
public class EmailServiceFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public EmailServiceFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that EmailService feature flag can be checked at runtime
    /// </summary>
    [Fact]
    public async Task EmailService_FeatureFlag_CanBeChecked()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.EmailService);

        // Assert
        // Currently disabled - Mock email service is the default
        Assert.False(isEnabled, "EmailService should be disabled by default (using Mock)");
    }

    /// <summary>
    /// Validates that EmailService flag has correct defaults per environment
    /// </summary>
    [Theory]
    [InlineData("Development", false)] // Use Mock in development
    [InlineData("Testing", false)]     // Use Mock for tests
    [InlineData("Production", false)]  // Use Mock in production until real service is ready
    public void EmailService_ShouldHaveCorrectDefaults_PerEnvironment(string environment, bool expectedState)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var emailServiceEnabled = configuration.GetValue<bool>("FeatureManagement:EmailService");

        // Assert
        Assert.Equal(expectedState, emailServiceEnabled);
    }

    /// <summary>
    /// Validates that email configuration exists in appsettings
    /// </summary>
    [Fact]
    public void EmailConfiguration_ShouldExist_InAppsettings()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Act
        var emailSection = configuration.GetSection("Email");

        // Assert
        Assert.NotNull(emailSection);
        Assert.True(emailSection.Exists(), "Email configuration section should exist");
    }

    /// <summary>
    /// Validates that email provider is configured
    /// </summary>
    [Theory]
    [InlineData("Development", "Mock")]
    [InlineData("Testing", "Mock")]
    [InlineData("Production", "Mock")] // Will change to "SendGrid" or "Smtp" when enabled
    public void EmailProvider_ShouldBeConfigured_PerEnvironment(string environment, string expectedProvider)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var provider = configuration.GetValue<string>("Email:Provider");

        // Assert
        Assert.Equal(expectedProvider, provider);
    }

    /// <summary>
    /// Validates that application starts correctly with Mock email service
    /// </summary>
    [Fact]
    public async Task Application_ShouldStart_WithMockEmailService()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        // This validates that the app starts correctly with Mock email service
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
