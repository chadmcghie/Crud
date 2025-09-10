# Spec Tasks

> Parent Issue: #94 - Caching Sub-Task 4: API Response Caching
> Parent Spec: Redis Caching Layer (Task 4 of 5)

## Tasks

- [x] 1. Output Caching Infrastructure Setup
  - [x] 1.1 Write tests for output caching middleware configuration
  - [x] 1.2 Configure ASP.NET Core output caching in Program.cs
  - [x] 1.3 Create named cache policies (PeoplePolicy, RolesPolicy, WallsPolicy, WindowsPolicy)
  - [x] 1.4 Integrate output cache store with existing Redis infrastructure
  - [x] 1.5 Configure fallback to in-memory cache when Redis unavailable
  - [x] 1.6 Verify output caching middleware tests pass

- [x] 2. HTTP Response Headers Implementation
  - [x] 2.1 Write tests for HTTP response header middleware
  - [x] 2.2 Create middleware for adding Cache-Control headers
  - [x] 2.3 Implement ETag generation based on entity timestamps
  - [x] 2.4 Add Last-Modified header based on entity UpdatedAt fields
  - [x] 2.5 Configure header middleware in request pipeline
  - [x] 2.6 Verify response header tests pass

- [x] 3. Controller Attribute Application
  - [x] 3.1 Write integration tests for cached endpoints
  - [x] 3.2 Apply [OutputCache] attributes to People GET endpoints
  - [x] 3.3 Apply [OutputCache] attributes to Roles GET endpoints
  - [x] 3.4 Apply [OutputCache] attributes to Walls GET endpoints
  - [x] 3.5 Apply [OutputCache] attributes to Windows GET endpoints
  - [x] 3.6 Configure vary-by parameters for query strings (page, size, filter)
  - [ ] 3.7 Verify endpoint caching integration tests pass

- [ ] 4. Conditional Request Support
  - [ ] 4.1 Write tests for conditional request handling
  - [ ] 4.2 Implement If-None-Match header processing for ETag validation
  - [ ] 4.3 Implement If-Modified-Since header processing for timestamp validation
  - [ ] 4.4 Add 304 Not Modified response logic to controllers
  - [ ] 4.5 Create action filter for conditional request handling
  - [ ] 4.6 Verify conditional request tests pass

- [ ] 5. Cache Invalidation Integration
  - [ ] 5.1 Write tests for cache invalidation on mutations
  - [ ] 5.2 Extend POST endpoint handlers to invalidate collection cache
  - [ ] 5.3 Extend PUT endpoint handlers to invalidate entity and collection cache
  - [ ] 5.4 Extend DELETE endpoint handlers to invalidate entity and collection cache
  - [ ] 5.5 Create cache invalidation service for output cache
  - [ ] 5.6 Integrate with existing MediatR command handlers
  - [ ] 5.7 Verify cache invalidation tests pass

- [ ] 6. Performance Testing & Validation
  - [ ] 6.1 Write E2E tests for complete caching workflow
  - [ ] 6.2 Create performance benchmarks for cached vs uncached endpoints
  - [ ] 6.3 Verify <50ms response time for cached requests
  - [ ] 6.4 Validate 70%+ cache hit ratio under load
  - [ ] 6.5 Test cache invalidation completes within 10ms
  - [ ] 6.6 Verify proper cache headers in browser developer tools
  - [ ] 6.7 Document caching behavior and configuration options

## Acceptance Criteria

- [ ] All GET endpoints return responses in <50ms when cached
- [ ] Cache-Control, ETag, and Last-Modified headers present on all GET responses
- [ ] 304 Not Modified returned for unchanged resources with conditional requests
- [ ] Cache automatically invalidates when data is modified via POST/PUT/DELETE
- [ ] Output caching integrates seamlessly with existing Redis infrastructure
- [ ] All unit, integration, and E2E tests pass
- [ ] Performance benchmarks meet specified targets

## Dependencies

- Existing Redis caching infrastructure (Task 1-3 completed)
- ASP.NET Core 8.0 output caching middleware
- Entity timestamps (UpdatedAt fields) for ETag/Last-Modified generation

## Notes

- This is Task 4 of the Redis Caching Layer implementation
- Builds upon the existing ICacheService and Redis infrastructure
- Should reuse the CompositeCacheService for Redis/LazyCache fallback
- Cache durations should be configurable via appsettings.json
- Consider implementing cache warming for frequently accessed data in future phase
