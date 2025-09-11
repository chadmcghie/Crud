using Api.Middleware;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Api.Extensions;

/// <summary>
/// Extension methods for configuring output caching
/// </summary>
public static class OutputCachingExtensions
{
    /// <summary>
    /// Adds output caching with Redis support and named policies
    /// </summary>
    public static IServiceCollection AddApiOutputCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind caching settings
        services.Configure<CachingSettings>(configuration.GetSection("Caching"));

        var cachingSettings = configuration.GetSection("Caching").Get<CachingSettings>() ?? new CachingSettings();

        // Check if output caching is disabled (for testing)
        var outputCachingDisabled = configuration.GetValue<bool>("OutputCaching:Disabled");
        if (outputCachingDisabled)
        {
            return services; // Skip output caching configuration
        }

        // Add output caching
        services.AddOutputCache(options =>
        {
            // Configure default policy
            options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);

            // Add named policies for each entity type
            options.AddPolicy("PeoplePolicy", builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(cachingSettings.PeopleCacheDurationSeconds))
                       .SetVaryByQuery("page", "size", "filter", "sort")
                       .SetVaryByRouteValue("id")
                       .Tag("people");
            });

            options.AddPolicy("RolesPolicy", builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(cachingSettings.RolesCacheDurationSeconds))
                       .SetVaryByQuery("page", "size", "filter", "sort")
                       .SetVaryByRouteValue("id")
                       .Tag("roles");
            });

            options.AddPolicy("WallsPolicy", builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(cachingSettings.WallsCacheDurationSeconds))
                       .SetVaryByQuery("page", "size", "filter", "sort")
                       .SetVaryByRouteValue("id")
                       .Tag("walls");
            });

            options.AddPolicy("WindowsPolicy", builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(cachingSettings.WindowsCacheDurationSeconds))
                       .SetVaryByQuery("page", "size", "filter", "sort")
                       .SetVaryByRouteValue("id")
                       .Tag("windows");
            });

            // Configure size limits
            options.SizeLimit = 100 * 1024 * 1024; // 100MB
            options.MaximumBodySize = 10 * 1024 * 1024; // 10MB per response
        });

        // Add Redis output cache store if Redis is configured
        if (cachingSettings.UseRedis)
        {
            try
            {
                // Try to use existing Redis connection if available
                services.AddSingleton<IOutputCacheStore>(serviceProvider =>
                {
                    var connectionMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
                    if (connectionMultiplexer != null && connectionMultiplexer.IsConnected)
                    {
                        return new RedisOutputCacheStore(connectionMultiplexer, serviceProvider.GetRequiredService<ILogger<RedisOutputCacheStore>>());
                    }

                    // Fall back to in-memory cache
                    var logger = serviceProvider.GetRequiredService<ILogger<RedisOutputCacheStore>>();
                    logger.LogWarning("Redis is not available for output caching. Falling back to in-memory cache.");
                    return new MemoryOutputCacheStore(serviceProvider.GetRequiredService<IOptions<OutputCacheOptions>>());
                });
            }
            catch
            {
                // Fall back to in-memory cache if Redis configuration fails
                services.AddSingleton<IOutputCacheStore, MemoryOutputCacheStore>();
            }
        }
        else
        {
            // Use in-memory cache store
            services.AddSingleton<IOutputCacheStore, MemoryOutputCacheStore>();
        }

        return services;
    }

    /// <summary>
    /// Adds middleware for HTTP response headers (Cache-Control, ETag, Last-Modified)
    /// </summary>
    public static IApplicationBuilder UseHttpCacheHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpCacheHeadersMiddleware>();
    }

    /// <summary>
    /// Adds middleware to track output cache hits
    /// </summary>
    public static IApplicationBuilder UseOutputCacheTracking(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Set initial cache status to MISS
            context.Items["CacheHit"] = false;

            // Hook into the output cache feature if available
            var outputCacheFeature = context.Features.Get<IOutputCacheFeature>();
            if (outputCacheFeature != null)
            {
                // The output cache feature will be present if output caching is enabled
                // We'll check after the response to see if it was a cache hit
                context.Response.OnStarting(() =>
                {
                    // If the response is being served from cache, mark it as a hit
                    // This is a simplified check - in production you'd want more sophisticated logic
                    if (context.Response.Headers.ContainsKey("X-OutputCache-Hit"))
                    {
                        context.Items["CacheHit"] = true;
                    }
                    return Task.CompletedTask;
                });
            }

            await next();
        });
    }
}
