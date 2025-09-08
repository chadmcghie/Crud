namespace Infrastructure.Services.Caching;

public class LazyCacheOptions
{
    public CacheDefaults DefaultCachePolicy { get; set; } = new();
}

public class CacheDefaults
{
    public int DefaultCacheDurationSeconds { get; set; } = 300; // 5 minutes default
}