---
id: BI-2025-08-31-001
status: active
category: test
severity: high
created: 2025-08-31 09:45
resolved: 
spec: test-server-optimization
task: Fix E2E test failures in CI pipeline
---

# E2E Tests Failing in CI After URL Fixes

## Problem Statement
The 3 minimal E2E tests (API health check, get person, get role) are failing in the GitHub Actions CI pipeline despite passing locally. This is blocking PR validation and merges.

## Symptoms
- Tests pass locally with servers running
- Tests fail in CI environment (Docker container)
- Affects PR validation workflow
- Error occurs in End-to-End Tests job
- GitHub Actions run: https://github.com/chadmcghie/Crud/actions/runs/17351644226/job/49258762742

## Impact
- PR validation blocked
- Cannot merge changes to main branch
- Development velocity impacted
- CI/CD pipeline reliability affected

## Root Cause Analysis (Five Whys)
1. Why are E2E tests failing in CI? Tests cannot connect to API server properly
2. Why can't tests connect to API? Network binding or URL resolution issues in Docker container
3. Why are there network issues? Docker container networking differs from local environment
4. Why does this difference matter? API binds to 0.0.0.0 but tests may not resolve correctly
5. Why isn't this resolved? Incomplete understanding of Docker container networking in GitHub Actions (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-08-31 08:00]
**Approach**: Changed API binding to 0.0.0.0 in CI environments
**Result**: Partial success - API health check passes but other tests fail
**Files Modified**: 
- test/Tests.E2E.NG/tests/setup/optimized-global-setup.ts
**Key Learning**: Docker containers need 0.0.0.0 binding for external access

### Attempt 2: [2025-08-31 08:30]
**Approach**: Fixed hardcoded localhost URLs to use relative URLs
**Result**: Tests still failing - likely connection issues remain
**Files Modified**:
- test/Tests.E2E.NG/tests/helpers/api-helpers.ts
- test/Tests.E2E.NG/tests/helpers/polly-api-helpers.ts
**Key Learning**: Relative URLs work with baseURL from APIRequestContext

### Attempt 3: [2025-08-31 09:00]
**Approach**: Fixed database cleanup timeout by using reset endpoint
**Result**: Timeout fixed but connection issues persist
**Files Modified**:
- test/Tests.E2E.NG/tests/setup/api-only-fixture.ts
**Key Learning**: Manual cleanup was too slow, database reset endpoint is faster

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: test/Tests.E2E.NG/tests/setup/optimized-global-setup.ts - Lines: 186-188 - Change: Bind API to 0.0.0.0 in CI - Reason: Required for Docker container networking
- [x] File: test/Tests.E2E.NG/tests/helpers/api-helpers.ts - Lines: 187-334 - Change: Use relative URLs instead of hardcoded localhost - Reason: Works with any baseURL configuration
- [x] File: test/Tests.E2E.NG/tests/setup/api-only-fixture.ts - Lines: 35-51 - Change: Use database reset endpoint - Reason: Prevents timeouts from slow manual cleanup
- [x] File: .github/workflows/pr-validation.yml - Lines: 327-330 - Change: Removed cache from Docker container setup - Reason: Fixes Post Setup .NET errors

## Current Workaround
Tests pass locally but not in CI. Can validate changes locally before pushing.

### Attempt 4: [2025-08-31 10:00]
**Hypothesis**: Tests are failing due to lack of visibility into what's happening - need debug logging
**Approach**: Add comprehensive debug logging to understand connection issues
**Implementation**:
- Added logging to show API URL being used
- Added logging for each step of person/role creation
- Added logging to server startup process
**Result**: Pending - will show exact failure point in CI
**Files Modified**:
- test/Tests.E2E.NG/tests/minimal-e2e.spec.ts (lines 5-76): Added debug logging
- test/Tests.E2E.NG/tests/setup/optimized-global-setup.ts (lines 226-237): Added connection logging
**Key Learning**: Will reveal if issue is with URL resolution, server startup, or API calls
**Next Direction**: Based on logs, will know exact failure point

### Attempt 5: [2025-08-31 10:30]
**Hypothesis**: API binds to 0.0.0.0 in CI but tests connect to localhost, which may not resolve correctly in Docker
**Approach**: Use 127.0.0.1 instead of localhost for API URL in CI environments
**Implementation**:
```typescript
// In CI (Docker), bind to 0.0.0.0 and connect via 127.0.0.1 for consistency
const apiUrl = process.env.CI ? `http://127.0.0.1:${apiPort}` : `http://localhost:${apiPort}`;
```
**Result**: Pending - will test in CI
**Files Modified**:
- test/Tests.E2E.NG/tests/setup/optimized-global-setup.ts (lines 156-157): Use 127.0.0.1 in CI
**Key Learning**: Docker containers may have issues resolving localhost vs 127.0.0.1
**Next Direction**: If this doesn't work, may need to use host.docker.internal or container networking

## Next Steps
- [x] Add debug logging to understand exact connection failures
- [x] Fix localhost vs 127.0.0.1 resolution in CI
- [ ] Verify API is actually accessible at expected URL in CI
- [ ] Test with explicit wait for server startup

## Related Issues
- Previous blocker: BI-2025-08-30-001 (Smoke test auth failure - resolved)
- Previous blocker: BI-2025-08-30-002 (E2E timeouts - partially resolved)
- GitHub Actions run: https://github.com/chadmcghie/Crud/actions/runs/17351644226