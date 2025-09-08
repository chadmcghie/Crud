using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = GetExpiry(options);
            
            await _database.StringSetAsync(key, serialized, expiry);
            _logger.LogDebug("Set cache value for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cache value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> factory, 
        CacheEntryOptions options, 
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            var value = await factory(cancellationToken);
            if (value != null)
            {
                await SetAsync(key, value, options, cancellationToken);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = new List<RedisKey>();
                
                await foreach (var key in server.KeysAsync(pattern: pattern))
                {
                    keys.Add(key);
                }
                
                if (keys.Any())
                {
                    await _database.KeyDeleteAsync(keys.ToArray());
                    _logger.LogDebug("Removed {Count} keys matching pattern: {Pattern}", keys.Count, pattern);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var keyArray = keys.ToArray();
            var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            
            var result = new Dictionary<string, T?>();
            for (int i = 0; i < keyArray.Length; i++)
            {
                if (values[i].HasValue)
                {
                    result[keyArray[i]] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions);
                }
                else
                {
                    result[keyArray[i]] = null;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple values from Redis");
            throw;
        }
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var tasks = new List<Task>();
            var expiry = GetExpiry(options);
            
            foreach (var kvp in items)
            {
                var serialized = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                tasks.Add(_database.StringSetAsync(kvp.Key, serialized, expiry));
            }
            
            await Task.WhenAll(tasks);
            _logger.LogDebug("Set {Count} cache values", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple values in Redis");
            throw;
        }
    }

    private TimeSpan? GetExpiry(CacheEntryOptions options)
    {
        if (options.AbsoluteExpiration.HasValue)
        {
            return options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
        }
        
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            return options.AbsoluteExpirationRelativeToNow.Value;
        }
        
        if (options.SlidingExpiration.HasValue)
        {
            return options.SlidingExpiration.Value;
        }
        
        return null;
    }
}