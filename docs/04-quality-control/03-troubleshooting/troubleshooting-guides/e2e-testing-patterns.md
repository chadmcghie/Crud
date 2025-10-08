# E2E Testing Patterns: Best Practices

This document outlines best practices for E2E testing, including authentication setup, event-driven patterns, and avoiding setTimeout-based delays.

## Table of Contents

1. [Test Authentication Patterns](#test-authentication-patterns)
2. [Angular Unit Tests - Event-Driven Patterns](#angular-unit-tests---event-driven-patterns)
3. [E2E Tests - Playwright Event-Driven Patterns](#e2e-tests---playwright-event-driven-patterns)
4. [Process Management - Promise.race Patterns](#process-management---promiserace-patterns)
5. [API Helper Retry Logic](#api-helper-retry-logic)
6. [Key Principles](#key-principles)

## Test Authentication Patterns

### ✅ Correct: Using setupTestAuthentication() Helper

**As of 2025-10-07**, all E2E tests should use the dedicated `setupTestAuthentication()` helper from `tests/helpers/test-auth-setup.ts`. This approach:
- Injects authentication before Angular loads using `page.addInitScript()`
- Keeps test-specific code out of production Angular files
- Provides consistent, reliable authentication across all tests
- Better security (no test backdoors in production code)

```typescript
// ✅ GOOD: Proper test authentication setup
import { setupTestAuthentication, clearTestAuthentication } from './helpers/test-auth-setup';

test.beforeEach(async ({ page }) => {
  // Set up authentication BEFORE navigating to the app
  await setupTestAuthentication(page, {
    userId: 'test-user-123',
    email: 'test@example.com',
    roles: ['User', 'Admin'],
    tokenExpirySeconds: 3600
  });

  // Now navigate - Angular will see the auth tokens
  await page.goto('/');
});

test.afterEach(async ({ page }) => {
  // Clean up authentication state
  await clearTestAuthentication(page);
});
```

### ❌ Incorrect: Production Code E2E Detection

**DO NOT** add E2E detection logic to production Angular services or components:

```typescript
// ❌ BAD: E2E detection in production code (REMOVED in cleanup)
export class AuthService {
  constructor() {
    // NEVER DO THIS - pollutes production code with test logic
    if (this.isE2ETestEnvironment()) {
      this.autoAuthenticateE2EUser();
    }
  }

  private isE2ETestEnvironment(): boolean {
    // Checking user agent, ports, localStorage - BAD!
    return navigator.userAgent.includes('Playwright') ||
           localStorage.getItem('e2e-test-mode') === 'active';
  }
}
```

### ❌ Incorrect: Inline Test Authentication

**DO NOT** manually inject authentication in test fixtures:

```typescript
// ❌ BAD: Inline authentication injection (DEPRECATED)
test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('e2e-test-mode', 'active');
    const mockUser = { id: 'test', email: 'test@test.com' };
    localStorage.setItem('user', JSON.stringify(mockUser));
    // ... 40+ lines of manual token creation
  });
});
```

### Event-Driven Wait Methods

Use the helper methods from `tests/helpers/page-helpers.ts` instead of `page.waitForTimeout()`:

```typescript
// ✅ GOOD: Event-driven wait methods
import { PageHelpers } from './helpers/page-helpers';

test('navigation test', async ({ page }) => {
  const helpers = new PageHelpers(page);

  // Wait for Angular navigation to complete
  await helpers.waitForNavigationComplete();

  // Wait for specific API data to load
  await helpers.waitForDataLoad('/api/people');

  // Wait for Angular component to be ready
  await helpers.waitForComponentReady('app-people-list');

  // Wait for form submission to complete
  await helpers.waitForFormSubmission('/api/people', 'POST');
});
```

```typescript
// ❌ BAD: Arbitrary timeouts (REMOVED in cleanup)
test('navigation test', async ({ page }) => {
  await page.click('a[href="/people"]');
  await page.waitForTimeout(500); // Arbitrary delay - FLAKY!

  await page.fill('input#name', 'Test');
  await page.waitForTimeout(1000); // Another arbitrary delay - FLAKY!
});
```

## Angular Unit Tests - Event-Driven Patterns

### ✅ Correct: Using fakeAsync and tick()

```typescript
// ✅ GOOD: Event-driven with fakeAsync/tick
it('should queue requests during token refresh', fakeAsync(() => {
  const refreshSubject = new Subject<TokenResponse>();
  authService.refreshToken.and.returnValue(refreshSubject.asObservable());
  
  // Start requests
  interceptor.intercept(firstRequest, next1).subscribe();
  interceptor.intercept(secondRequest, next2).subscribe();
  
  // Advance virtual time deterministically
  tick();
  
  // Trigger refresh completion
  refreshSubject.next({ accessToken: 'new-token', refreshToken: 'new-refresh' });
  refreshSubject.complete();
  
  // Process completion
  tick();
  
  // Assertions
  expect(authService.refreshToken).toHaveBeenCalledTimes(1);
}));
```

### ❌ Incorrect: Using setTimeout

```typescript
// ❌ BAD: setTimeout causes flaky, non-deterministic tests
it('should handle async operation', async () => {
  // Start async operation
  service.startOperation();
  
  // Wait arbitrary time - FLAKY!
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  // Test might pass or fail depending on timing
  expect(service.isComplete()).toBe(true);
});
```

## E2E Tests - Playwright Event-Driven Patterns

### ✅ Correct: Using expect.toPass() for retries

```typescript
// ✅ GOOD: Event-driven retry with expect.toPass()
test('should update existing wall', async ({ apiHelpers, page }) => {
  const createdWall = await apiHelpers.createWall(testWall);
  await apiHelpers.updateWall(createdWall.id, updatedData);
  
  // Use Playwright's built-in retry mechanism
  await expect(async () => {
    const retrievedWall = await apiHelpers.getWall(createdWall.id);
    expect(retrievedWall).toMatchObject(updatedData);
  }).toPass({
    timeout: 5000,
    intervals: [100, 250, 500] // Deterministic retry intervals
  });
});
```

### ✅ Correct: Using page.waitFor() patterns

```typescript
// ✅ GOOD: Event-driven polling with expect.toPass()
const waitForServer = async (url: string, timeout: number): Promise<boolean> => {
  try {
    await expect(async () => {
      const response = await page.request.get(url);
      expect(response.ok()).toBe(true);
    }).toPass({
      timeout: timeout,
      intervals: [500, 1000] // Check every 500ms, then every 1s
    });
    return true;
  } catch {
    return false;
  }
};
```

### ❌ Incorrect: Manual setTimeout polling

```typescript
// ❌ BAD: Manual polling with setTimeout
const waitForServer = (url: string, timeout: number): Promise<boolean> => {
  return new Promise((resolve) => {
    const checkServer = async () => {
      try {
        const response = await page.request.get(url);
        if (response.ok()) {
          resolve(true);
          return;
        }
      } catch {}
      
      // Manual timeout and polling - FLAKY!
      setTimeout(checkServer, 500);
    };
    checkServer();
  });
};
```

## Process Management - Promise.race Patterns

### ✅ Correct: Event-driven process cleanup

```typescript
// ✅ GOOD: Event-driven cleanup with Promise.race
await Promise.race([
  new Promise<void>((resolve) => {
    process.once('exit', resolve);
  }),
  new Promise<void>((resolve) => {
    // Deterministic timeout using Promise chains
    Promise.resolve()
      .then(() => new Promise(r => process.nextTick(r)))
      .then(() => {
        if (!process.killed) {
          process.kill('SIGKILL');
        }
        resolve();
      });
  })
]);
```

### ❌ Incorrect: setTimeout cleanup

```typescript
// ❌ BAD: setTimeout-based cleanup
await new Promise<void>((resolve) => {
  const timeout = setTimeout(() => {
    if (!process.killed) {
      process.kill('SIGKILL');
    }
    resolve();
  }, 1000);
  
  process.once('exit', () => {
    clearTimeout(timeout);
    resolve();
  });
});
```

## API Helper Retry Logic

### ✅ Correct: Simplified retry with reduced delays

```typescript
// ✅ GOOD: Simplified, deterministic retry logic
private async retryOperation<T>(operation: () => Promise<T>): Promise<T> {
  const maxRetries = 3;
  const baseDelay = 100;
  
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await operation();
    } catch (error) {
      if (attempt === maxRetries || !shouldRetry(error)) {
        throw error;
      }
      
      // Simple linear backoff instead of exponential
      const delay = baseDelay * (attempt + 1);
      await this.sleep(delay);
    }
  }
}
```

### ❌ Incorrect: Complex exponential backoff

```typescript
// ❌ BAD: Complex exponential backoff with jitter
private async retryOperation<T>(operation: () => Promise<T>): Promise<T> {
  // Complex delay calculation with jitter makes tests non-deterministic
  let delay = baseDelayMs * Math.pow(2, attempt);
  delay = Math.min(delay, maxDelayMs);
  
  if (useJitter) {
    const jitter = delay * 0.25 * (Math.random() * 2 - 1);
    delay += jitter; // Random jitter makes tests flaky!
  }
  
  await this.sleep(delay);
}
```

## Key Principles

### For Angular Unit Tests
1. **Always use `fakeAsync` and `tick()`** for time-dependent tests
2. **Use `flush()`** to ensure all async operations complete
3. **Use `TestScheduler`** for complex RxJS observable testing
4. **Never use `setTimeout`** in unit tests

### For E2E Tests (Playwright)
1. **Use `expect.toPass()`** for retry logic with deterministic intervals
2. **Use `page.waitFor()`** patterns instead of manual polling
3. **Use `Promise.race()`** for timeout scenarios
4. **Minimize setTimeout usage** - prefer Playwright's built-in mechanisms

### General Guidelines
1. **Deterministic over random** - avoid jitter and random delays
2. **Event-driven over time-based** - wait for actual conditions, not arbitrary time
3. **Built-in mechanisms over manual** - use framework retry patterns
4. **Fast feedback loops** - shorter, more predictable test times

## Migration Checklist

### Completed Refactorings

- [x] ✅ **Test Authentication** (2025-10-07) - Created setupTestAuthentication() helper
  - Removed 187 lines of E2E detection from production Angular code
  - Removed isE2ETestEnvironment() from auth.service.ts
  - Removed data-e2e-ready attributes from app.ts
  - Removed TestAuthService from auth.guard.ts
  - Updated all 4 test fixtures to use new helper

- [x] ✅ **Event-Driven Waits** (2025-10-07) - Replaced 14 timer-based waits
  - Created waitForNavigationComplete() method
  - Created waitForDataLoad() method
  - Created waitForComponentReady() method
  - Created waitForFormSubmission() method
  - Updated 6 test files to use event-driven patterns

- [x] ✅ **Angular Unit Tests** - Using fakeAsync/tick for time-dependent tests

- [x] ✅ **E2E Test Retry Logic** - Using expect.toPass() with deterministic intervals

- [x] ✅ **Server Polling Patterns** - Using expect.toPass() instead of manual polling

- [x] ✅ **Process Cleanup** - Using Promise.race for event-driven cleanup

- [x] ✅ **API Helper Retry Logic** - Simplified with linear backoff

### Future Improvements

- [ ] 🔄 Consider RxJS TestScheduler for complex observable scenarios
- [ ] 🔄 Add more examples as patterns are discovered
- [ ] 🔄 Fix flaky person CRUD test (complete-user-workflows.spec.ts:69)
- [ ] 🔄 Improve full-workflow.spec.ts database cleanup

## Benefits

These refactorings make tests:
- **More reliable** - No race conditions or timing dependencies
- **Faster** - No arbitrary delays slowing down test execution
- **More secure** - Zero test-specific code in production
- **Easier to maintain** - Centralized test infrastructure
- **Deterministic** - Predictable, reproducible results

## References

- E2E Cleanup Spec: `docs/03-development/specs/2025-10-07-e2e-test-cleanup/`
- Implementation Summary: `docs/03-development/specs/2025-10-07-e2e-test-cleanup/IMPLEMENTATION_SUMMARY.md`
- Test Auth Helper: `test/Tests.E2E.NG/tests/helpers/test-auth-setup.ts`
- Page Helpers: `test/Tests.E2E.NG/tests/helpers/page-helpers.ts`