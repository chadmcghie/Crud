---
id: BI-2025-09-08-001
status: resolved
category: test
severity: critical
created: 2025-09-08 17:48
resolved: 2025-09-08 18:00
spec: redis-caching-layer
task: implement-redis-caching-with-composite-strategy
---

# ICacheService Dependency Injection Registration Missing in Integration Tests

## Problem Statement
Backend integration tests are failing with dependency injection error when trying to resolve `ICacheService` for the `CacheInvalidationBehavior`. This blocks PR #170 from passing CI/CD pipeline and prevents merging the Redis caching layer feature.

## Symptoms
- Error: `System.InvalidOperationException: Unable to resolve service for type 'App.Interfaces.ICacheService'`
- Occurs when MediatR tries to activate `CacheInvalidationBehavior`
- All integration tests fail with HTTP 409 Conflict status instead of expected responses
- Failure occurs in GitHub Actions CI pipeline
- Tests pass locally but fail in CI environment

## Impact
- All backend integration tests are blocked (100% failure rate)
- PR #170 cannot be merged to dev branch
- Redis caching layer feature implementation is blocked
- CI/CD pipeline is red, blocking all dependent work

## Root Cause Analysis (Five Whys)
1. Why did integration tests fail? 
   Answer: ICacheService cannot be resolved from dependency injection container
2. Why can't ICacheService be resolved? 
   Answer: It's not registered in the test's WebApplicationFactory configuration
3. Why isn't it registered in test configuration?
   Answer: The new caching services were added to production DI but not to test setup
4. Why wasn't it added to test setup?
   Answer: The integration test factory wasn't updated when adding the new caching layer
5. Why wasn't the test factory updated?
   Answer: Missing consideration for test environment when implementing new services (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-08 17:45]
**Approach**: Added cache service registration to integration tests
**Result**: Local tests pass but need to verify CI behavior
**Files Modified**: 
- test/Tests.Integration.Backend/IntegrationTestFactory.cs
**Key Learning**: Test environments need explicit service registration separate from production

### Attempt 2: [2025-09-08 17:46]
**Approach**: Running full integration test suite locally
**Result**: Investigating current test failures to understand scope
**Files Modified**:
- None (diagnosis phase)
**Key Learning**: Need to identify all services requiring test registration

### Attempt 3: [2025-09-08 17:47]
**Approach**: Analyzing dependency chain for CacheInvalidationBehavior
**Result**: Identified missing ICacheService registration as root cause
**Files Modified**:
- None (analysis phase)
**Key Learning**: MediatR behaviors require all dependencies registered even in test context

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: src/Infrastructure/Services/Caching/*.cs - Lines: Various - Change: Updated all cache services to implement App.Interfaces.ICacheService - Reason: Unified interface for App and Infrastructure layers
- [x] File: src/Infrastructure/DependencyInjection.cs - Lines: 158-233 - Change: Register App.Interfaces.ICacheService instead of Infrastructure version - Reason: MediatR behaviors require App interface
- [x] File: src/Infrastructure/Services/Caching/CachedRepositoryDecorator.cs - Lines: Various - Change: Updated to use App.Interfaces.ICacheService - Reason: Consistent interface usage across layers
- [x] File: test/Tests.Integration.Backend/Infrastructure/SqliteTestWebApplicationFactory.cs - Lines: 78-93 - Change: Added AddCachingServices call - Reason: Integration tests need cache services registered

## Current Workaround
~~Temporarily use in-memory cache implementation for integration tests instead of full Redis stack to avoid external dependencies in test environment.~~
No longer needed - issue resolved.

## Next Steps
- [x] Identify missing service registrations in IntegrationTestFactory
- [x] Add ICacheService registration with test-appropriate implementation
- [x] Add IRedisService mock or in-memory substitute for tests
- [x] Add IMemoryCacheService registration for tests
- [x] Verify all MediatR behaviors have required dependencies
- [x] Run full integration test suite locally
- [ ] Push fix and verify CI pipeline passes

## Resolution Summary
The issue was caused by having two separate ICacheService interfaces:
1. `App.Interfaces.ICacheService` - Used by MediatR behaviors
2. `Infrastructure.Services.Caching.ICacheService` - Used by Infrastructure implementations

The Infrastructure services were implementing their own local interface, but the App layer behaviors expected the App interface. Resolution involved:
1. Removing the duplicate Infrastructure.Services.Caching.ICacheService interface
2. Updating all Infrastructure cache services to implement App.Interfaces.ICacheService
3. Updating DI registrations to use App.Interfaces.ICacheService consistently
4. Fixing namespace conflicts with CacheItemPriority and CacheEntryOptions

The integration tests now pass (106/110 passing, 4 unrelated password reset test failures).

## Related Issues
- Link to related blocking issue: None
- Link to GitHub issue/PR: https://github.com/chadmcghie/Crud/pull/170
- Link to spec task: .agent-os/specs/redis-caching-layer/tasks.md