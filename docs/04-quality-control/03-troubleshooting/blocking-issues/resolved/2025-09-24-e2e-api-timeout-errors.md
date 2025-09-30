# BI-2025-09-24-003: E2E API Timeout Errors

## Issue Summary
**ID**: BI-2025-09-24-003
**Created**: 2025-09-24
**Resolved**: 2025-09-25
**Severity**: MEDIUM
**Category**: Performance
**Status**: RESOLVED

## Description
E2E tests are experiencing API timeout errors where requests exceed the 10000ms (10 second) timeout limit. The API endpoints are not responding within the expected timeframe, causing test failures.

## Symptoms
```
⚠️  getPeople failed (attempt 1/4), retrying in 100ms: apiRequestContext.get: Timeout 10000ms exceeded.
Call log:
  - → GET http://localhost:5172/api/people
    - user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.7339.16 Safari/537.36
    - accept: application/json
    - accept-encoding: gzip,deflate,br
    - Content-Type: application/json
    - Authorization: Bearer [JWT_TOKEN]
    - X-E2E-Test: true
    - cookie: refreshToken=[REFRESH_TOKEN]
```

## Root Cause Analysis
API requests to `/api/people` and potentially other endpoints are hanging and not completing within the 10-second timeout. Possible causes:

1. **Database Lock Issues**: SQLite database locks preventing query completion
2. **Server Performance Issues**: API server under load or misconfigured
3. **Authentication/Authorization Delays**: JWT token validation taking excessive time
4. **Test Infrastructure Problems**: Network connectivity issues in test environment
5. **Concurrent Request Conflicts**: Multiple tests accessing same resources simultaneously

## Impact
- E2E tests failing due to API timeouts
- Test suite execution time significantly increased due to retries
- Affects both data retrieval and cleanup operations
- Blocks comprehensive E2E validation

## Affected Operations
- GET `/api/people` requests
- Potentially other API endpoints
- Test cleanup operations (people cleanup, role cleanup)

## Technical Analysis
The timeout is happening during:
- Data retrieval operations
- Test cleanup phases
- Background API helper retry mechanisms are triggering (attempt 1/4)

## Timeout Configuration
Current timeout settings:
- API request timeout: 10000ms (10 seconds)
- Helper retry attempts: 4 attempts with backoff

## Resolution Options
1. **Increase Timeout**: Extend API timeout limit for test environment
2. **Fix Database Issues**: Resolve SQLite locking or performance problems
3. **Optimize API Performance**: Improve endpoint response times
4. **Improve Test Isolation**: Better cleanup and resource management between tests
5. **Add Endpoint Health Checks**: Verify API health before running tests

## Related Issues
- May be related to BI-2025-09-23-009 (multi-provider integration test failures)
- Could be connected to database provider issues

## Context
This issue emerged after resolving E2E navigation issues, revealing API performance problems that were previously masked by navigation failures. The serial test execution strategy may be exposing resource contention issues.

## Resolution (2025-09-25)

Resolved as part of comprehensive E2E test ecosystem fix (**BI-2025-09-25-001**).

**Evidence**: E2E smoke tests 45/45 passing with no timeout errors reported.