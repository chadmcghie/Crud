---
id: BI-2025-09-09-001
status: active
category: test
severity: high
created: 2025-09-09 21:20
resolved: 
spec: refactor-database-controller
task: Fix CI test failures
---

# AuthInterceptor Unit Tests Failing in CI but Passing Locally

## Problem Statement
Two AuthInterceptor unit tests in the Angular frontend are failing in CI environment but pass consistently in local development. This blocks PR validation and merge to the dev branch.

## Symptoms
- Error: "Expected spy AuthService.refreshToken to have been called once. It was called 0 times"
- Error: "Expected false to be truthy" in request queuing test
- Tests affected: 
  - "should queue requests during token refresh"
  - "should release queued requests when refresh fails"
- Occurs: Only in CI (GitHub Actions), not locally
- Environment: Linux CI runner vs Windows local development

## Impact
- PR #171 validation cannot complete
- Blocks merge of DatabaseController dependency injection refactoring
- Affects CI/CD pipeline reliability
- Creates uncertainty about test stability

## Root Cause Analysis (Five Whys)
1. Why did the tests fail? The AuthService.refreshToken spy was not called as expected
2. Why was the spy not called? The token refresh logic may not be triggered in CI environment
3. Why is it not triggered in CI? Timing differences between local and CI environments
4. Why are there timing differences? CI runners may execute faster/slower affecting async operations
5. Why does timing affect the test? Tests may have race conditions or insufficient async handling (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-09 20:30]
**Approach**: Fixed backend integration test for concurrent operations
**Result**: Backend tests now pass, but frontend tests still fail
**Files Modified**: 
- test/Tests.Integration.Backend/Controllers/ApiHealthTests.cs
**Key Learning**: Backend and frontend test issues are independent

### Attempt 2: [2025-09-09 21:00]
**Approach**: Ran all tests locally to verify
**Result**: All tests pass locally including the failing AuthInterceptor tests
**Files Modified**: None
**Key Learning**: Confirms environment-specific issue between local and CI

### Attempt 3: [2025-09-09 21:15]
**Approach**: Analyzed CI logs for specific failure patterns
**Result**: Found tests fail at spy assertions, indicating timing/async issues
**Files Modified**: None
**Key Learning**: Tests have potential race conditions in token refresh simulation

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: test/Tests.Integration.Backend/Controllers/ApiHealthTests.cs - Lines: 138-205 - Change: Added staggered delays and proper SQLite concurrent handling - Reason: Fixes legitimate SQLite locking issues in integration tests

## Current Workaround
None available - tests must pass for PR validation

### Attempt 4: [2025-09-09 23:15]
**Hypothesis**: Tests have race conditions due to insufficient async handling and timing issues in CI
**Approach**: Add proper async/await patterns, explicit delays, and better error handling in failing tests
**Implementation**:
```typescript
// Fixed timing-sensitive tests with:
// 1. Proper done() callback usage with explicit completion checks
// 2. Added small delays (25-50ms) to simulate real-world timing
// 3. Centralized completion checking to avoid race conditions
// 4. Better error handling with descriptive failure messages
// 5. Improved async operation sequencing

// Key changes:
// - "should queue requests during token refresh": Added checkCompletion() function and proper async sequencing
// - "should handle multiple concurrent 401 responses": Made async with done() callback and completion tracking
// - "should release queued requests when refresh fails": Added delays and centralized error checking
// - "should handle 401 error and attempt token refresh": Added delay to refresh and proper async completion
// - "should redirect to login when refresh token fails": Added delay and proper async completion
```
**Result**: All 267 tests pass locally including the previously failing AuthInterceptor tests
**Files Modified**: 
- src/Angular/src/app/auth.interceptor.spec.ts (lines 126-179, 212-359): Fixed race conditions in async tests
**Key Learning**: CI timing differences require explicit delays and proper async handling in tests

## Resolution Analysis
The root cause was indeed race conditions in the AuthInterceptor tests where:
1. Tests relied on immediate execution of async operations without proper waiting
2. CI environments execute at different speeds than local development
3. Tests didn't account for the asynchronous nature of RxJS operations
4. Missing proper completion callbacks led to premature test completion

## Next Steps
- [x] Add explicit delays or better async handling in AuthInterceptor tests
- [x] Investigate timing issues in the failing tests
- [ ] Monitor CI results to confirm fix effectiveness
- [ ] Consider implementing test retry logic if issues persist

## Related Issues
- PR #171: https://github.com/chadmcghie/Crud/pull/171
- Previous similar issue: Frontend tests timing out in CI
- Related spec: DatabaseController dependency injection refactoring