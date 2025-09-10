using System.Buffers;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

namespace Api.Extensions;

/// <summary>
/// Redis-backed implementation of IOutputCacheStore for distributed output caching
/// </summary>
public class RedisOutputCacheStore : IOutputCacheStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisOutputCacheStore> _logger;
    private const string KeyPrefix = "output_cache:";

    public RedisOutputCacheStore(IConnectionMultiplexer redis, ILogger<RedisOutputCacheStore> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            var value = await _database.StringGetAsync(fullKey);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Output cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Output cache hit for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting output cache value from Redis for key: {Key}", key);
            return null;
        }
    }

    public async ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            await _database.StringSetAsync(fullKey, value, validFor);
            
            // Store tags for cache invalidation
            if (tags != null && tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    var tagKey = $"{KeyPrefix}tag:{tag}";
                    await _database.SetAddAsync(tagKey, key);
                    await _database.KeyExpireAsync(tagKey, validFor);
                }
            }

            _logger.LogDebug("Output cache set for key: {Key} with expiration: {Expiration}", key, validFor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting output cache value in Redis for key: {Key}", key);
            // Don't throw - allow request to continue without caching
        }
    }

    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        try
        {
            var tagKey = $"{KeyPrefix}tag:{tag}";
            var keys = await _database.SetMembersAsync(tagKey);
            
            if (keys.Length > 0)
            {
                var keysToDelete = keys.Select(k => (RedisKey)$"{KeyPrefix}{k}").ToArray();
                await _database.KeyDeleteAsync(keysToDelete);
                await _database.KeyDeleteAsync(tagKey);
                
                _logger.LogDebug("Evicted {Count} cache entries for tag: {Tag}", keys.Length, tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evicting output cache by tag: {Tag}", tag);
            // Don't throw - allow request to continue
        }
    }
}
