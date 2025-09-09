---
id: BI-2025-09-09-002
status: active
category: test
severity: high
created: 2025-09-09 23:15
resolved: 
spec: refactor-database-controller
task: fix-angular-test-ci-failures
---

# Test Delay Anti-Pattern in AuthInterceptor Tests

## Problem Statement
AuthInterceptor tests are using arbitrary setTimeout delays (25ms, 50ms) to coordinate async operations instead of properly testing the event-driven RxJS code. This creates fragile, non-deterministic tests that fail intermittently in CI environments.

## Symptoms
- Tests pass locally on Windows but fail in CI on Ubuntu
- Intermittent test failures requiring arbitrary delay increases
- Tests use setTimeout to "ensure" operations complete
- Race conditions masked by timing delays rather than fixed

## Impact
- CI pipeline failures blocking PR merges
- False positives in test results
- Maintenance burden of adjusting delays for different environments
- Tests don't actually validate event-driven behavior

## Root Cause Analysis (Five Whys)
1. Why did tests fail in CI but not locally?
   Answer: CI environment (Ubuntu) executes at different speeds than local (Windows)

2. Why do execution speed differences cause failures?
   Answer: Tests rely on setTimeout delays to coordinate async operations

3. Why are tests using setTimeout instead of proper async testing?
   Answer: Quick fix applied to address CI failures without refactoring test approach

4. Why wasn't proper async testing used initially?
   Answer: Tests weren't designed to handle RxJS event streams properly

5. Why weren't RxJS testing utilities used?
   Answer: Lack of awareness/understanding of proper RxJS testing patterns (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-09 22:00]
**Approach**: Increased delays from 10ms to 25-50ms
**Result**: Tests pass but solution is fragile
**Files Modified**: 
- src/Angular/src/app/auth.interceptor.spec.ts
**Key Learning**: Delays are band-aid fixes, not real solutions

### Attempt 2: [2025-09-09 22:30]
**Approach**: Added centralized completion tracking with checkCompletion()
**Result**: Better organization but still relies on delays
**Files Modified**:
- src/Angular/src/app/auth.interceptor.spec.ts
**Key Learning**: Completion tracking helps but doesn't address root issue

### Attempt 3: [2025-09-09 23:00]
**Approach**: Added error handling and descriptive messages
**Result**: Better debugging but core delay problem remains
**Files Modified**:
- src/Angular/src/app/auth.interceptor.spec.ts
**Key Learning**: Need to refactor to use proper async testing utilities

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: src/Angular/src/app/auth.interceptor.spec.ts - Lines: 244-256 - Change: Added centralized checkCompletion() - Reason: Better tracking of async operations
- [x] File: src/Angular/src/app/auth.interceptor.spec.ts - Lines: 265, 276 - Change: Added descriptive error messages - Reason: Better debugging when tests fail
- [ ] File: src/Angular/src/app/auth.interceptor.spec.ts - Lines: 249, 269, 280 - Change: setTimeout delays - Reason: SHOULD BE REPLACED with proper async testing

## Current Workaround
Increased delays to 25-50ms which makes tests pass in CI, but this is fragile and not a proper solution.

## Proper Solution (To Implement)
Replace setTimeout delays with:
1. Angular's `fakeAsync` and `tick()` for deterministic time control
2. RxJS `TestScheduler` for marble testing of observables
3. Proper observable chaining instead of timing-based coordination

Example of correct approach:
```typescript
it('should queue requests during token refresh', fakeAsync(() => {
  // Setup
  const refreshSubject = new Subject<TokenResponse>();
  authService.refreshToken.and.returnValue(refreshSubject.asObservable());
  
  // Start requests
  interceptor.intercept(firstRequest, next1).subscribe();
  interceptor.intercept(secondRequest, next2).subscribe();
  
  // Advance virtual time
  tick();
  
  // Trigger refresh completion
  refreshSubject.next({ accessToken: 'new-token', refreshToken: 'new-refresh' });
  refreshSubject.complete();
  
  // Advance to process completion
  tick();
  
  // Assertions
  expect(authService.refreshToken).toHaveBeenCalledTimes(1);
}));
```

## Next Steps
- [ ] Refactor auth.interceptor.spec.ts to use fakeAsync/tick
- [ ] Consider using RxJS TestScheduler for complex observable testing
- [ ] Document proper RxJS testing patterns in project guidelines
- [ ] Review other tests for similar anti-patterns

## Related Issues
- Link to related blocking issue: BI-2025-09-09-001 (AuthInterceptor CI failures)
- Link to GitHub issue/PR: #171
- Link to spec task: refactor-database-controller-dependencies