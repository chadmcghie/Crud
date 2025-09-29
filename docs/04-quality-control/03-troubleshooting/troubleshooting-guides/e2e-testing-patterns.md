# E2E Testing Patterns: Event-Driven vs setTimeout

This document outlines the refactoring from setTimeout-based delays to event-driven patterns in our E2E and unit tests.

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

- [x] ✅ Angular unit tests (auth.interceptor.spec.ts) - using fakeAsync/tick
- [x] ✅ E2E test retry logic - using expect.toPass()
- [x] ✅ Server polling patterns - using expect.toPass() 
- [x] ✅ Process cleanup - using Promise.race
- [x] ✅ API helper retry logic - simplified with linear backoff
- [ ] 🔄 Consider RxJS TestScheduler for complex observable scenarios
- [ ] 🔄 Add more examples as patterns are discovered

This refactoring makes tests more reliable, faster, and deterministic.