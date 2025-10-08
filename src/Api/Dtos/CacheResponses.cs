namespace Api.Dtos;

public record CacheStatsResponse(
    double HitRatio,
    long TotalHits,
    long TotalMisses,
    long KeyCount,
    double MemoryUsageMB,
    bool RedisConnected,
    string Uptime,
    double AverageResponseTimeMs,
    Dictionary<string, long> MostAccessedKeys,
    Dictionary<string, long> HitsByType,
    Dictionary<string, long> MissesByType
);

public record CacheClearResponse(
    bool Cleared,
    long KeysRemoved,
    string Message
);

public record CacheHealthResponse(
    string Status,
    CacheRedisHealth Redis,
    CacheInMemoryHealth InMemory
);

public record CacheRedisHealth(
    bool Connected,
    double LatencyMs,
    string? Version
);

public record CacheInMemoryHealth(
    bool Available,
    int ItemCount
);

public record CacheKeyListResponse(
    IEnumerable<string> Keys,
    long TotalCount,
    string Pattern
);
