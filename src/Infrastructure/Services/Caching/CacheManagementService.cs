using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Interfaces;
using Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services.Caching;

public class CacheManagementService : ICacheManagementService
{
    private readonly ICacheService _cacheService;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CacheManagementService> _logger;

    public CacheManagementService(
        ICacheService cacheService,
        IConnectionMultiplexer? redis,
        ILogger<CacheManagementService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _redis = redis;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cache clear all operation");

            if (_redis?.IsConnected == true)
            {
                var database = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());

                // Use FLUSHDB to clear the current database
                await server.FlushDatabaseAsync();
                _logger.LogInformation("Successfully cleared Redis cache");
            }
            else
            {
                _logger.LogWarning("Redis not available for cache clear operation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all caches");
            throw;
        }
    }

    public async Task ClearByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cache clear by pattern");

            if (_redis?.IsConnected == true)
            {
                var database = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());

                // Use SCAN to find keys matching the pattern
                var keys = server.Keys(pattern: pattern);
                var keyArray = keys.ToArray();

                if (keyArray.Length > 0)
                {
                    await database.KeyDeleteAsync(keyArray);
                    _logger.LogInformation("Cleared {Count} keys matching pattern", keyArray.Length);
                }
                else
                {
                    _logger.LogInformation("No keys found matching pattern");
                }
            }
            else
            {
                _logger.LogWarning("Redis not available for pattern-based cache clear");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache by pattern");
            throw;
        }
    }

    public async Task RemoveKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing cache key");
            await _cacheService.RemoveAsync(key, cancellationToken);
            _logger.LogInformation("Successfully removed cache key");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key");
            throw;
        }
    }

    public async Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cacheService.ExistsAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists");
            throw;
        }
    }

    public async Task WarmCriticalDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cache warming for critical data");

            // This is a placeholder for cache warming logic
            // In a real implementation, this would preload frequently accessed data
            // For now, we'll just log that warming is requested

            // Example warming operations could include:
            // - Preload all roles (small, frequently accessed)
            // - Preload most recently accessed people
            // - Preload configuration data

            _logger.LogInformation("Cache warming completed - placeholder implementation");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
            throw;
        }
    }

    public async Task<long> GetKeyCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_redis?.IsConnected == true)
            {
                var database = _redis.GetDatabase();
                var result = await database.ExecuteAsync("DBSIZE");
                return (long)result;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key count");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_redis?.IsConnected == true)
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());

                // Use SCAN to safely iterate through keys
                var keys = server.Keys(pattern: pattern).Take(limit);
                return keys.Select(k => k.ToString());
            }

            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache keys with pattern");
            throw;
        }
    }
}
