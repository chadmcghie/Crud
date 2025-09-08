using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace App.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions options,
        CancellationToken cancellationToken = default) where T : class;

    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;

    Task SetManyAsync<T>(IDictionary<string, T> items, CacheEntryOptions options, CancellationToken cancellationToken = default) where T : class;
}

public class CacheEntryOptions
{
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    public TimeSpan? SlidingExpiration { get; set; }

    public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

    public long? Size { get; set; }

    public static CacheEntryOptions Default => new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    public static CacheEntryOptions NoExpiration => new();

    public static CacheEntryOptions FromMinutes(int minutes) => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
    };

    public static CacheEntryOptions FromHours(int hours) => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(hours)
    };

    public static CacheEntryOptions WithSliding(TimeSpan slidingExpiration) => new()
    {
        SlidingExpiration = slidingExpiration
    };
}

public enum CacheItemPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    NeverRemove = 3
}
