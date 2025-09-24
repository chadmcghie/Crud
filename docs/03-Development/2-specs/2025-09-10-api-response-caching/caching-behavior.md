# API Response Caching - Behavior and Configuration

## Overview
The API implements a comprehensive caching strategy using ASP.NET Core Output Caching with Redis as the primary store and in-memory caching as a fallback.

## Caching Behavior

### Cached Endpoints
All GET endpoints for the following entities are cached:
- `/api/people` and `/api/people/{id}` - 5 minutes (300 seconds)
- `/api/roles` and `/api/roles/{id}` - 10 minutes (600 seconds)  
- `/api/walls` and `/api/walls/{id}` - 5 minutes (300 seconds)
- `/api/windows` and `/api/windows/{id}` - 5 minutes (300 seconds)

### Cache Headers
All cached responses include:
- **ETag**: MD5 hash of the response content for validation
- **Last-Modified**: Timestamp of the most recent entity update
- **Cache-Control**: Directives for client-side caching (when configured)

### Conditional Requests
The API supports conditional requests to minimize bandwidth:
- **If-None-Match**: Returns 304 Not Modified if ETag matches
- **If-Modified-Since**: Returns 304 Not Modified if content hasn't changed

### Cache Invalidation
Caches are automatically invalidated when data changes:
- **POST**: Invalidates collection cache
- **PUT**: Invalidates both entity and collection cache
- **DELETE**: Invalidates both entity and collection cache

## Configuration

### appsettings.json
```json
{
  "Caching": {
    "UseRedis": true,
    "UseOutputCaching": true,
    "RedisConnectionString": "localhost:6379",
    "DefaultTtlMinutes": 5,
    "PeopleCacheDurationSeconds": 300,
    "RolesCacheDurationSeconds": 600,
    "WallsCacheDurationSeconds": 300,
    "WindowsCacheDurationSeconds": 300
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `UseRedis` | Enable Redis caching | `true` |
| `UseOutputCaching` | Enable output caching | `true` |
| `RedisConnectionString` | Redis server connection | `localhost:6379` |
| `DefaultTtlMinutes` | Default cache duration | `5` minutes |
| `PeopleCacheDurationSeconds` | People endpoint cache duration | `300` seconds |
| `RolesCacheDurationSeconds` | Roles endpoint cache duration | `600` seconds |
| `WallsCacheDurationSeconds` | Walls endpoint cache duration | `300` seconds |
| `WindowsCacheDurationSeconds` | Windows endpoint cache duration | `300` seconds |

## Performance Characteristics

### Response Times
- **Initial Request (Cache Miss)**: 50-200ms depending on data complexity
- **Cached Request (Cache Hit)**: <50ms (typically 5-20ms)
- **Conditional Request (304)**: <10ms

### Cache Hit Ratios
- **Read-Heavy Workloads**: >90% cache hit ratio
- **Balanced Workloads**: 70-85% cache hit ratio
- **Write-Heavy Workloads**: 40-60% cache hit ratio

### Invalidation Performance
- **Cache Invalidation Overhead**: <10ms per operation
- **No blocking**: Invalidation happens asynchronously

## Architecture

### Cache Stores
1. **Redis (Primary)**: Distributed cache for multi-instance scenarios
2. **In-Memory (Fallback)**: Used when Redis is unavailable
3. **Composite Pattern**: Automatic fallback between stores

### Components
- **OutputCachingExtensions**: Configures caching policies and middleware
- **ConditionalRequestFilter**: Handles If-None-Match and If-Modified-Since
- **OutputCacheInvalidationService**: Manages cache invalidation on mutations
- **RedisOutputCacheStore**: Custom IOutputCacheStore implementation for Redis
- **MemoryOutputCacheStore**: In-memory fallback implementation

## Monitoring and Debugging

### Logging
Cache operations are logged at the Debug level:
- Cache hits/misses
- Invalidation operations
- Store selection (Redis vs Memory)

### Headers for Debugging
- **X-Cache**: Indicates cache status (HIT/MISS) when enabled
- **ETag**: Unique identifier for response version
- **Last-Modified**: Timestamp of last data modification

## Best Practices

### When to Use Caching
✅ **Good Candidates**:
- Read-heavy endpoints
- Data that changes infrequently
- Expensive computations or aggregations
- Public or shared data

❌ **Poor Candidates**:
- User-specific data that changes frequently
- Real-time data requirements
- Sensitive data requiring immediate consistency

### Cache Duration Guidelines
- **Static Reference Data**: 10-30 minutes
- **Frequently Accessed Entities**: 5-10 minutes
- **Dynamic Business Data**: 1-5 minutes
- **User-Specific Data**: 30 seconds - 1 minute

### Troubleshooting

**Issue**: Cache not working
- Check Redis connection in logs
- Verify `UseOutputCaching` is `true` in appsettings
- Ensure controllers have `[OutputCache]` attributes

**Issue**: Stale data after updates
- Verify cache invalidation service is registered
- Check that mutations call invalidation methods
- Review invalidation logs for errors

**Issue**: Poor performance
- Monitor cache hit ratios
- Adjust cache durations based on usage patterns
- Consider increasing Redis memory allocation
- Review network latency to Redis server

## Testing

### Integration Tests
- `OutputCachingTests`: Basic caching functionality
- `ConditionalRequestTests`: ETag and If-Modified-Since handling
- `CacheInvalidationTests`: Mutation-triggered invalidation
- `CachingE2ETests`: End-to-end workflows and performance

### Performance Validation
Run the E2E tests to validate performance:
```bash
dotnet test --filter "FullyQualifiedName~CachingE2ETests"
```

Expected results:
- ✅ Cached responses under 50ms
- ✅ Cache hit ratio above 70%
- ✅ Cache invalidation under 10ms
- ✅ Proper cache headers on all endpoints
