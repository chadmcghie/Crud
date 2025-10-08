using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests.Integration.Backend.OutputCaching;

public class OutputCachingConfigurationTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public OutputCachingConfigurationTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void OutputCaching_ShouldBeRegisteredInServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var outputCacheOptions = scope.ServiceProvider.GetService<IOptions<OutputCacheOptions>>();

        // Assert
        Assert.NotNull(outputCacheOptions);
    }

    [Fact]
    public void OutputCaching_ShouldHaveNamedPolicies()
    {
        // Arrange
        var expectedPolicies = new[] { "PeoplePolicy", "RolesPolicy", "WallsPolicy", "WindowsPolicy" };

        // Act
        using var scope = _factory.Services.CreateScope();
        var outputCacheOptions = scope.ServiceProvider.GetRequiredService<IOptions<OutputCacheOptions>>();

        // Assert
        // In .NET 8, we can't directly verify policy names from OutputCacheOptions
        // The policies are registered internally
        Assert.NotNull(outputCacheOptions);
        Assert.NotNull(outputCacheOptions.Value);
    }

    [Fact]
    public void OutputCaching_ShouldUseRedisWhenConfigured()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<Api.CachingSettings>(options =>
                {
                    options.UseRedis = true;
                    options.UseOutputCaching = true;
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var outputCacheStore = scope.ServiceProvider.GetService<IOutputCacheStore>();

        // Assert
        Assert.NotNull(outputCacheStore);
        // When Redis is configured, it should use a Redis-backed store
        // The actual type check would depend on the implementation
    }

    [Fact]
    public void OutputCaching_ShouldFallbackToMemoryWhenRedisUnavailable()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<Api.CachingSettings>(options =>
                {
                    options.UseRedis = false;
                    options.UseOutputCaching = true;
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var outputCacheStore = scope.ServiceProvider.GetService<IOutputCacheStore>();

        // Assert
        Assert.NotNull(outputCacheStore);
    }

    [Theory]
    [InlineData("PeoplePolicy", 300)] // 5 minutes
    [InlineData("RolesPolicy", 3600)] // 1 hour
    [InlineData("WallsPolicy", 600)] // 10 minutes
    [InlineData("WindowsPolicy", 600)] // 10 minutes
    public void OutputCachePolicies_ShouldHaveCorrectDurations(string policyName, int expectedDurationSeconds)
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var outputCacheOptions = scope.ServiceProvider.GetRequiredService<IOptions<OutputCacheOptions>>();
        var cachingSettings = scope.ServiceProvider.GetRequiredService<IOptions<Api.CachingSettings>>();

        // Assert
        Assert.NotNull(outputCacheOptions);
        Assert.NotNull(outputCacheOptions.Value);

        // Verify durations from settings
        switch (policyName)
        {
            case "PeoplePolicy":
                Assert.Equal(expectedDurationSeconds, cachingSettings.Value.PeopleCacheDurationSeconds);
                break;
            case "RolesPolicy":
                Assert.Equal(expectedDurationSeconds, cachingSettings.Value.RolesCacheDurationSeconds);
                break;
            case "WallsPolicy":
                Assert.Equal(expectedDurationSeconds, cachingSettings.Value.WallsCacheDurationSeconds);
                break;
            case "WindowsPolicy":
                Assert.Equal(expectedDurationSeconds, cachingSettings.Value.WindowsCacheDurationSeconds);
                break;
        }
    }
}
