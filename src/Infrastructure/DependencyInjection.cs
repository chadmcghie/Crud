using App.Abstractions;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories.EntityFramework;
using Infrastructure.Services;
using Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure;

public static class DependencyInjection
{



    /// <summary>
    /// Adds infrastructure services using Entity Framework with SQL Server
    /// Suitable for production scenarios
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkSqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register generic repository pattern
        services.AddScoped(typeof(Domain.Interfaces.IRepository<>), typeof(EfRepository<>));

        // Register base repository implementations (will be wrapped by decorators if caching is enabled)
        services.AddScoped<EfPersonRepository>();
        services.AddScoped<EfRoleRepository>();
        services.AddScoped<EfWallRepository>();
        services.AddScoped<EfWindowRepository>();

        // Register repository interfaces (without caching - will be overridden by AddCachedRepositories if called)
        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        // Add authentication services
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add password reset services
        services.AddScoped<IPasswordResetTokenRepository, EfPasswordResetTokenRepository>();
        services.AddScoped<IEmailService, MockEmailService>();

        // Add database test service for testing scenarios
        services.AddScoped<DatabaseTestService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services using Entity Framework with SQLite
    /// Suitable for development, testing, and lightweight production scenarios
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkSqlite(this IServiceCollection services, string connectionString)
    {
        // In CI/Testing environments, optimize SQLite connection for single-use scenarios
        if (Environment.GetEnvironmentVariable("CI") == "true" ||
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
        {
            // Add parameters to reduce locking issues in CI
            if (!connectionString.Contains(";"))
            {
                connectionString += ";";
            }
            // Use Private cache to avoid shared cache locking issues
            if (!connectionString.Contains("Cache=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += "Cache=Private;";
            }
            // Disable connection pooling to ensure clean connections
            if (!connectionString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += "Pooling=False;";
            }
            if (!connectionString.Contains("Mode=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += "Mode=ReadWriteCreate;";
            }
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register generic repository pattern
        services.AddScoped(typeof(Domain.Interfaces.IRepository<>), typeof(EfRepository<>));

        // Register base repository implementations (will be wrapped by decorators if caching is enabled)
        services.AddScoped<EfPersonRepository>();
        services.AddScoped<EfRoleRepository>();
        services.AddScoped<EfWallRepository>();
        services.AddScoped<EfWindowRepository>();

        // Register repository interfaces (without caching - will be overridden by AddCachedRepositories if called)
        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        // Add authentication services
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add password reset services
        services.AddScoped<IPasswordResetTokenRepository, EfPasswordResetTokenRepository>();
        services.AddScoped<IEmailService, MockEmailService>();

        // Add database test service for testing scenarios
        services.AddScoped<DatabaseTestService>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations
    /// Call this during application startup for Entity Framework configurations
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            await context.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Adds caching services based on configuration
    /// Supports Redis, LazyCache, InMemory, and Composite caching strategies
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cachingConfig = configuration.GetSection("Caching");
        var useRedis = cachingConfig.GetValue<bool>("UseRedis");
        var useLazyCache = cachingConfig.GetValue<bool>("UseLazyCache");
        var useComposite = cachingConfig.GetValue<bool>("UseComposite");

        // Add MemoryCache for InMemoryCacheService
        services.AddMemoryCache();

        // Configure LazyCache options
        services.Configure<LazyCacheOptions>(configuration.GetSection("Caching:LazyCache"));

        // Register cache infrastructure services (for repository-level caching)
        // Using fully qualified names to avoid conflicts with App layer services
        services.AddSingleton<Infrastructure.Services.Caching.ICacheKeyGenerator, Infrastructure.Services.Caching.CacheKeyGenerator>();
        services.AddSingleton<ICacheConfiguration, CacheConfiguration>();

        // Register cache services based on configuration
        if (useComposite && useRedis)
        {
            // Setup Redis connection
            var redisConfig = configuration.GetSection("Redis");
            var connectionString = redisConfig.GetValue<string>("ConnectionString") ?? "localhost:6379";
            var connectTimeout = redisConfig.GetValue<int?>("ConnectTimeout") ?? 5000;
            var syncTimeout = redisConfig.GetValue<int?>("SyncTimeout") ?? 5000;
            var abortOnConnectFail = redisConfig.GetValue<bool?>("AbortOnConnectFail") ?? false;

            var redisOptions = ConfigurationOptions.Parse(connectionString);
            redisOptions.ConnectTimeout = connectTimeout;
            redisOptions.SyncTimeout = syncTimeout;
            redisOptions.AbortOnConnectFail = abortOnConnectFail;

            // Register Redis connection as singleton
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(redisOptions);
                }
                catch (Exception ex)
                {
                    var logger = provider.GetService<ILogger<IConnectionMultiplexer>>();
                    logger?.LogError(ex, "Failed to connect to Redis. Falling back to in-memory cache.");
                    throw;
                }
            });

            // Register individual cache services
            services.AddSingleton<RedisCacheService>();
            services.AddSingleton<InMemoryCacheService>();
            services.AddSingleton<LazyCacheService>();

            // Register composite cache as the primary App.Interfaces.ICacheService
            services.AddSingleton<App.Interfaces.ICacheService>(provider =>
            {
                var primaryCache = provider.GetRequiredService<RedisCacheService>();
                var fallbackCache = useLazyCache
                    ? (App.Interfaces.ICacheService)provider.GetRequiredService<LazyCacheService>()
                    : provider.GetRequiredService<InMemoryCacheService>();
                var logger = provider.GetRequiredService<ILogger<CompositeCacheService>>();

                return new CompositeCacheService(primaryCache, fallbackCache, logger);
            });
        }
        else if (useRedis)
        {
            // Setup Redis connection
            var redisConfig = configuration.GetSection("Redis");
            var connectionString = redisConfig.GetValue<string>("ConnectionString") ?? "localhost:6379";
            var connectTimeout = redisConfig.GetValue<int?>("ConnectTimeout") ?? 5000;
            var syncTimeout = redisConfig.GetValue<int?>("SyncTimeout") ?? 5000;
            var abortOnConnectFail = redisConfig.GetValue<bool?>("AbortOnConnectFail") ?? false;

            var redisOptions = ConfigurationOptions.Parse(connectionString);
            redisOptions.ConnectTimeout = connectTimeout;
            redisOptions.SyncTimeout = syncTimeout;
            redisOptions.AbortOnConnectFail = abortOnConnectFail;

            services.AddSingleton<IConnectionMultiplexer>(provider => ConnectionMultiplexer.Connect(redisOptions));
            services.AddSingleton<App.Interfaces.ICacheService, RedisCacheService>();
        }
        else if (useLazyCache)
        {
            services.AddSingleton<App.Interfaces.ICacheService, LazyCacheService>();
        }
        else
        {
            // Default to in-memory cache
            services.AddSingleton<App.Interfaces.ICacheService, InMemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Adds cached repository decorators to wrap existing repositories with caching functionality
    /// </summary>
    public static IServiceCollection AddCachedRepositories(this IServiceCollection services)
    {
        // Register cached repository decorators
        services.AddScoped<IPersonRepository>(provider =>
        {
            var repository = provider.GetRequiredService<EfPersonRepository>();
            var cacheService = provider.GetRequiredService<App.Interfaces.ICacheService>();
            var keyGenerator = provider.GetRequiredService<Infrastructure.Services.Caching.ICacheKeyGenerator>();
            var cacheConfig = provider.GetRequiredService<ICacheConfiguration>();

            return new CachedPersonRepositoryDecorator(
                repository, cacheService, keyGenerator, cacheConfig);
        });

        services.AddScoped<IRoleRepository>(provider =>
        {
            var repository = provider.GetRequiredService<EfRoleRepository>();
            var cacheService = provider.GetRequiredService<App.Interfaces.ICacheService>();
            var keyGenerator = provider.GetRequiredService<Infrastructure.Services.Caching.ICacheKeyGenerator>();
            var cacheConfig = provider.GetRequiredService<ICacheConfiguration>();

            return new CachedRoleRepositoryDecorator(
                repository, cacheService, keyGenerator, cacheConfig);
        });

        services.AddScoped<IWallRepository>(provider =>
        {
            var repository = provider.GetRequiredService<EfWallRepository>();
            var cacheService = provider.GetRequiredService<App.Interfaces.ICacheService>();
            var keyGenerator = provider.GetRequiredService<Infrastructure.Services.Caching.ICacheKeyGenerator>();
            var cacheConfig = provider.GetRequiredService<ICacheConfiguration>();

            return new CachedWallRepositoryDecorator(
                repository, cacheService, keyGenerator, cacheConfig);
        });

        services.AddScoped<IWindowRepository>(provider =>
        {
            var repository = provider.GetRequiredService<EfWindowRepository>();
            var cacheService = provider.GetRequiredService<App.Interfaces.ICacheService>();
            var keyGenerator = provider.GetRequiredService<Infrastructure.Services.Caching.ICacheKeyGenerator>();
            var cacheConfig = provider.GetRequiredService<ICacheConfiguration>();

            return new CachedWindowRepositoryDecorator(
                repository, cacheService, keyGenerator, cacheConfig);
        });

        return services;
    }
}
