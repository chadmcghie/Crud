# API Specification

This is the API specification for the spec detailed in [spec.md](../spec.md)

## Endpoints

### GET /api/cache/stats

**Purpose:** Retrieve current cache statistics including hit/miss ratios, memory usage, and key counts
**Parameters:** None
**Response:** 
```json
{
  "hitRatio": 0.85,
  "totalHits": 15234,
  "totalMisses": 2680,
  "memoryUsageMB": 124.5,
  "keyCount": 3421,
  "redisConnected": true,
  "uptime": "2:14:32"
}
```
**Errors:** 
- 500: Cache service unavailable
- 503: Statistics collection timeout

### POST /api/cache/clear

**Purpose:** Clear all cached items or specific cache regions
**Parameters:** 
- region (optional): Specific cache region to clear (e.g., "users", "products")
- pattern (optional): Key pattern to match for deletion (e.g., "user:*")
**Response:** 
```json
{
  "cleared": true,
  "keysRemoved": 1523,
  "message": "Cache cleared successfully"
}
```
**Errors:**
- 400: Invalid region or pattern specified
- 401: Unauthorized (requires admin role)
- 500: Cache service error

### DELETE /api/cache/key/{key}

**Purpose:** Remove a specific cache key
**Parameters:** 
- key (required): The cache key to remove
**Response:** 204 No Content
**Errors:**
- 401: Unauthorized (requires admin role)
- 404: Key not found
- 500: Cache service error

### GET /api/cache/health

**Purpose:** Health check endpoint for cache services
**Parameters:** None
**Response:**
```json
{
  "status": "Healthy",
  "redis": {
    "connected": true,
    "latencyMs": 2.3,
    "version": "7.0.5"
  },
  "inMemory": {
    "available": true,
    "itemCount": 245
  }
}
```
**Errors:**
- 503: Service degraded (partial functionality)
- 500: Service unhealthy

### GET /api/cache/config

**Purpose:** Retrieve current cache configuration settings
**Parameters:** None
**Response:**
```json
{
  "defaultTTL": 300,
  "slidingExpiration": true,
  "redisConnectionString": "redis://***",
  "enableCompression": true,
  "maxMemoryMB": 512,
  "regions": [
    {
      "name": "users",
      "ttl": 600,
      "priority": "high"
    }
  ]
}
```
**Errors:**
- 401: Unauthorized (requires admin role)
- 500: Configuration read error

## Cache Headers

All cacheable GET endpoints will include the following response headers:

- **X-Cache**: "HIT" or "MISS" indicating cache status
- **X-Cache-Key**: The generated cache key used
- **X-Cache-TTL**: Remaining TTL in seconds
- **Cache-Control**: Standard HTTP cache control directives
- **ETag**: Entity tag for conditional requests
- **Last-Modified**: Timestamp of last data modification

## Output Caching Configuration

GET endpoints supporting output caching will use the following attributes:

```csharp
[OutputCache(Duration = 300, VaryByQueryKeys = new[] { "page", "size" })]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
```

## Authentication & Authorization

- Cache statistics endpoints: Require authenticated user
- Cache management endpoints (clear, delete): Require Admin role
- Health check endpoint: Anonymous access allowed
- Configuration endpoint: Require Admin role

## Rate Limiting

Cache management operations are subject to rate limiting:
- Clear operations: 1 per minute per user
- Individual key deletion: 10 per minute per user
- Statistics queries: 60 per minute per user