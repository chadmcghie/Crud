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

## Next Steps
- [ ] Add explicit delays or better async handling in AuthInterceptor tests
- [ ] Investigate fakeAsync/tick usage in the failing tests
- [ ] Consider adding retry logic for flaky tests in CI
- [ ] Review if recent Angular or dependency updates affected timing

## Related Issues
- PR #171: https://github.com/chadmcghie/Crud/pull/171
- Previous similar issue: Frontend tests timing out in CI
- Related spec: DatabaseController dependency injection refactoring