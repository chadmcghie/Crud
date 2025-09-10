using System.Collections.Concurrent;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Extensions;

/// <summary>
/// In-memory implementation of IOutputCacheStore for single-instance output caching
/// </summary>
public class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _taggedKeys;
    private readonly ILogger<MemoryOutputCacheStore> _logger;

    public MemoryOutputCacheStore(IOptions<OutputCacheOptions> options)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.SizeLimit
        });
        _taggedKeys = new ConcurrentDictionary<string, HashSet<string>>();
        _logger = new LoggerFactory().CreateLogger<MemoryOutputCacheStore>();
    }

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out byte[]? value))
        {
            _logger.LogDebug("Memory output cache hit for key: {Key}", key);
            return new ValueTask<byte[]?>(value);
        }

        _logger.LogDebug("Memory output cache miss for key: {Key}", key);
        return new ValueTask<byte[]?>((byte[]?)null);
    }

    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = validFor,
            Size = value.Length
        };

        _cache.Set(key, value, cacheEntryOptions);

        // Store tags for cache invalidation
        if (tags != null && tags.Length > 0)
        {
            foreach (var tag in tags)
            {
                _taggedKeys.AddOrUpdate(tag,
                    new HashSet<string> { key },
                    (_, set) =>
                    {
                        set.Add(key);
                        return set;
                    });
            }
        }

        _logger.LogDebug("Memory output cache set for key: {Key} with expiration: {Expiration}", key, validFor);
        return ValueTask.CompletedTask;
    }

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        if (_taggedKeys.TryRemove(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }

            _logger.LogDebug("Evicted {Count} cache entries for tag: {Tag}", keys.Count, tag);
        }

        return ValueTask.CompletedTask;
    }
}
