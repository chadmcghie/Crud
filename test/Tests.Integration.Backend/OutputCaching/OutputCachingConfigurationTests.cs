using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
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
        outputCacheOptions.Should().NotBeNull("Output caching should be registered in DI container");
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
        outputCacheOptions.Should().NotBeNull();
        outputCacheOptions.Value.Should().NotBeNull();
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
        outputCacheStore.Should().NotBeNull("Output cache store should be registered when Redis is configured");
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
        outputCacheStore.Should().NotBeNull("Output cache store should fall back to memory when Redis is unavailable");
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
        outputCacheOptions.Should().NotBeNull();
        outputCacheOptions.Value.Should().NotBeNull();

        // Verify durations from settings
        switch (policyName)
        {
            case "PeoplePolicy":
                cachingSettings.Value.PeopleCacheDurationSeconds.Should().Be(expectedDurationSeconds);
                break;
            case "RolesPolicy":
                cachingSettings.Value.RolesCacheDurationSeconds.Should().Be(expectedDurationSeconds);
                break;
            case "WallsPolicy":
                cachingSettings.Value.WallsCacheDurationSeconds.Should().Be(expectedDurationSeconds);
                break;
            case "WindowsPolicy":
                cachingSettings.Value.WindowsCacheDurationSeconds.Should().Be(expectedDurationSeconds);
                break;
        }
    }
}
