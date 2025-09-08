using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Caching;

public class LazyCacheService : ICacheService
{
    private readonly IAppCache _cache;
    private readonly ILogger<LazyCacheService> _logger;
    private readonly LazyCacheOptions _options;

    public LazyCacheService(IOptions<LazyCacheOptions> options, ILogger<LazyCacheService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new CachingService();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = await Task.FromResult(_cache.Get<T>(key));
            
            if (result == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from LazyCache for key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var memoryCacheOptions = ConvertToMemoryCacheOptions(options);
            _cache.Add(key, value, memoryCacheOptions);
            _logger.LogDebug("Set cache value for key: {Key}", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in LazyCache for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache value for key: {Key}", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from LazyCache for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _cache.TryGetValue<object>(key, out _);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in LazyCache for key: {Key}", key);
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
            var memoryCacheOptions = ConvertToMemoryCacheOptions(options);
            
            // LazyCache's GetOrAddAsync provides thread-safe lazy loading
            var result = await _cache.GetOrAddAsync(
                key,
                async entry =>
                {
                    entry.SetOptions(memoryCacheOptions);
                    return await factory(cancellationToken);
                });
            
            _logger.LogDebug("GetOrSet completed for key: {Key}", key);
            return result;
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
            // LazyCache doesn't have built-in pattern removal
            // This is a limitation compared to Redis
            // In production, you might want to track keys separately or use a different strategy
            _logger.LogWarning("RemoveByPatternAsync is not fully supported by LazyCache. Pattern: {Pattern}", pattern);
            await Task.CompletedTask;
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
            var result = new Dictionary<string, T?>();
            
            foreach (var key in keys)
            {
                result[key] = _cache.Get<T>(key);
            }
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple values from LazyCache");
            throw;
        }
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var memoryCacheOptions = ConvertToMemoryCacheOptions(options);
            
            foreach (var kvp in items)
            {
                _cache.Add(kvp.Key, kvp.Value, memoryCacheOptions);
            }
            
            _logger.LogDebug("Set {Count} cache values", items.Count);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple values in LazyCache");
            throw;
        }
    }

    private MemoryCacheEntryOptions ConvertToMemoryCacheOptions(CacheEntryOptions options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions();
        
        if (options.AbsoluteExpiration.HasValue)
        {
            memoryCacheOptions.AbsoluteExpiration = options.AbsoluteExpiration;
        }
        
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            memoryCacheOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        }
        
        if (options.SlidingExpiration.HasValue)
        {
            memoryCacheOptions.SlidingExpiration = options.SlidingExpiration;
        }
        
        // Convert our custom priority enum to Microsoft's
        memoryCacheOptions.Priority = options.Priority switch
        {
            CacheItemPriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
            CacheItemPriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
            CacheItemPriority.NeverRemove => Microsoft.Extensions.Caching.Memory.CacheItemPriority.NeverRemove,
            _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
        };
        
        if (options.Size.HasValue)
        {
            memoryCacheOptions.Size = options.Size;
        }
        
        return memoryCacheOptions;
    }
}