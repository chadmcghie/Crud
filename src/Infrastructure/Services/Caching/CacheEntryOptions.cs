using System;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services.Caching;

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