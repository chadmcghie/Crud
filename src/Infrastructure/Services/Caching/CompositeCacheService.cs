using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Caching;

public class CompositeCacheService : ICacheService
{
    private readonly ICacheService _primaryCache;
    private readonly ICacheService _fallbackCache;
    private readonly ILogger<CompositeCacheService> _logger;

    public CompositeCacheService(
        ICacheService primaryCache,
        ICacheService fallbackCache,
        ILogger<CompositeCacheService> logger)
    {
        _primaryCache = primaryCache ?? throw new ArgumentNullException(nameof(primaryCache));
        _fallbackCache = fallbackCache ?? throw new ArgumentNullException(nameof(fallbackCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try primary cache first
            var result = await _primaryCache.GetAsync<T>(key, cancellationToken);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary cache failed for key: {Key}, falling back to secondary cache", key);
        }

        // Fallback to secondary cache
        try
        {
            return await _fallbackCache.GetAsync<T>(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Both primary and fallback caches failed for key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var tasks = new List<Task>();
        
        // Try to set in primary cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _primaryCache.SetAsync(key, value, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set value in primary cache for key: {Key}", key);
            }
        }, cancellationToken));

        // Always set in fallback cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _fallbackCache.SetAsync(key, value, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set value in fallback cache for key: {Key}", key);
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        
        // Remove from primary cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _primaryCache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove value from primary cache for key: {Key}", key);
            }
        }, cancellationToken));

        // Remove from fallback cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _fallbackCache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove value from fallback cache for key: {Key}", key);
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _primaryCache.ExistsAsync(key, cancellationToken))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary cache failed checking existence for key: {Key}", key);
        }

        try
        {
            return await _fallbackCache.ExistsAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Both primary and fallback caches failed checking existence for key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key, 
        Func<CancellationToken, Task<T>> factory, 
        CacheEntryOptions options, 
        CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from either cache
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Not in either cache, generate value
        var value = await factory(cancellationToken);
        if (value != null)
        {
            // Set in both caches
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        
        // Remove from primary cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _primaryCache.RemoveByPatternAsync(pattern, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove by pattern from primary cache: {Pattern}", pattern);
            }
        }, cancellationToken));

        // Remove from fallback cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _fallbackCache.RemoveByPatternAsync(pattern, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove by pattern from fallback cache: {Pattern}", pattern);
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await _primaryCache.GetManyAsync<T>(keys, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary cache failed getting multiple values, falling back to secondary cache");
            
            try
            {
                return await _fallbackCache.GetManyAsync<T>(keys, cancellationToken);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Both primary and fallback caches failed getting multiple values");
                throw;
            }
        }
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class
    {
        var tasks = new List<Task>();
        
        // Set in primary cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _primaryCache.SetManyAsync(items, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set multiple values in primary cache");
            }
        }, cancellationToken));

        // Set in fallback cache
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                await _fallbackCache.SetManyAsync(items, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set multiple values in fallback cache");
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
    }
}