# API Specification

This is the API specification for the spec detailed in @docs/03-Development/specs/2025-09-10-api-response-caching/spec.md

## HTTP Response Headers

### Cache-Control Headers
All GET endpoints will include appropriate Cache-Control headers:
- **People endpoints**: `Cache-Control: public, max-age=300` (5 minutes)
- **Roles endpoints**: `Cache-Control: public, max-age=3600` (1 hour)  
- **Walls endpoints**: `Cache-Control: public, max-age=600` (10 minutes)
- **Windows endpoints**: `Cache-Control: public, max-age=600` (10 minutes)

### ETag Headers
All GET endpoints will include ETag headers based on:
- **Single entity**: `ETag: "entity-id-timestamp"`
- **Collection**: `ETag: "collection-hash-timestamp"`

### Last-Modified Headers
All GET endpoints will include Last-Modified headers:
- **Format**: `Last-Modified: Wed, 21 Oct 2015 07:28:00 GMT`
- **Source**: Entity UpdatedAt timestamp or collection max timestamp

## Conditional Request Support

### If-None-Match Header
**Purpose:** Enable client-side caching with ETag validation
**Behavior:** Return 304 Not Modified if ETag matches current entity version
**Example Request:**
```
GET /api/people/1
If-None-Match: "1-2025-09-10T10:30:00Z"
```
**Response:** 304 Not Modified (if unchanged) or 200 OK with updated data

### If-Modified-Since Header  
**Purpose:** Enable client-side caching with timestamp validation
**Behavior:** Return 304 Not Modified if entity not modified since specified date
**Example Request:**
```
GET /api/people/1
If-Modified-Since: Wed, 10 Sep 2025 10:30:00 GMT
```
**Response:** 304 Not Modified (if unchanged) or 200 OK with updated data

## Cache Invalidation Endpoints

### Automatic Invalidation
Cache entries are automatically invalidated when:
- **POST /api/people** - Invalidates people collection cache
- **PUT /api/people/{id}** - Invalidates specific person and collection cache
- **DELETE /api/people/{id}** - Invalidates specific person and collection cache

### Manual Cache Management (Future)
Reserved endpoints for cache management:
- **DELETE /api/cache/people** - Clear all people-related cache entries
- **DELETE /api/cache/all** - Clear all cache entries

## Output Cache Policies

### Policy Configuration
Named cache policies applied via [OutputCache] attributes:

```csharp
[OutputCache(PolicyName = "PeoplePolicy")]
public async Task<ActionResult<PersonDto>> GetPerson(int id)

[OutputCache(PolicyName = "RolesPolicy")] 
public async Task<ActionResult<List<RoleDto>>> GetRoles()
```

### Policy Definitions
- **PeoplePolicy**: 5-minute cache, vary by route values
- **RolesPolicy**: 1-hour cache, vary by route values  
- **WallsPolicy**: 10-minute cache, vary by route values
- **WindowsPolicy**: 10-minute cache, vary by route values
