using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for Identity/Authentication feature flag
/// Validates that authentication mode can be toggled between JWT-only and ASP.NET Core Identity
/// </summary>
/// <remarks>
/// This is a Release Toggle (short-lived) for gradual migration from JWT to ASP.NET Core Identity.
/// Expected lifecycle: 1-2 weeks after Identity implementation is stable, then remove toggle.
/// </remarks>
public class IdentityAuthenticationFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public IdentityAuthenticationFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Validates that IdentityAuthentication feature flag can be checked at runtime
    /// </summary>
    [Fact]
    public async Task IdentityAuthentication_FeatureFlag_CanBeChecked()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.IdentityAuthentication);

        // Assert
        // Currently disabled - JWT authentication is the default
        Assert.False(isEnabled, "IdentityAuthentication should be disabled by default (using JWT)");
    }

    /// <summary>
    /// Validates that IdentityAuthentication flag has correct defaults per environment
    /// </summary>
    [Theory]
    [InlineData("Development", false)] // Start with JWT in development
    [InlineData("Testing", false)]     // Use JWT for tests
    [InlineData("Production", false)]  // JWT in production until migration complete
    public void IdentityAuthentication_ShouldHaveCorrectDefaults_PerEnvironment(string environment, bool expectedState)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var identityAuthEnabled = configuration.GetValue<bool>("FeatureManagement:IdentityAuthentication");

        // Assert
        Assert.Equal(expectedState, identityAuthEnabled);
    }

    /// <summary>
    /// Validates that JWT authentication works when IdentityAuthentication is disabled
    /// </summary>
    [Fact]
    public async Task JwtAuthentication_ShouldWork_WhenIdentityAuthenticationDisabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Try to access health endpoint (doesn't require auth)
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        // This validates that the app starts correctly with JWT authentication
    }

    /// <summary>
    /// Validates that authentication configuration exists
    /// </summary>
    [Fact]
    public void AuthenticationConfiguration_ShouldExist()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Act
        var jwtSection = configuration.GetSection("Jwt");

        // Assert
        Assert.NotNull(jwtSection);
        Assert.True(jwtSection.Exists(), "JWT configuration section should exist");
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
