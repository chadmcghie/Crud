using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Caching;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(IMemoryCache memoryCache, ILogger<InMemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from MemoryCache for key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var memoryCacheOptions = ConvertToMemoryCacheOptions(options);
            _memoryCache.Set(key, value, memoryCacheOptions);
            _logger.LogDebug("Set cache value for key: {Key}", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in MemoryCache for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache value for key: {Key}", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from MemoryCache for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in MemoryCache for key: {Key}", key);
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
            if (_memoryCache.TryGetValue(key, out T? cached))
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
        // MemoryCache doesn't support pattern-based removal natively
        // This is a limitation - in production, you might track keys separately
        _logger.LogWarning("RemoveByPatternAsync is not supported by InMemoryCacheService. Pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = new Dictionary<string, T?>();

            foreach (var key in keys)
            {
                _memoryCache.TryGetValue(key, out T? value);
                result[key] = value;
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple values from MemoryCache");
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
                _memoryCache.Set(kvp.Key, kvp.Value, memoryCacheOptions);
            }

            _logger.LogDebug("Set {Count} cache values", items.Count);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple values in MemoryCache");
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
