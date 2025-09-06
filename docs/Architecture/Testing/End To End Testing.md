# End-to-End (E2E) Testing Strategy

## Overview

**Scope**: The entire system as a user would experience it.

**Goal**: Validate that business workflows work correctly from the user's perspective.

**Tools**: Playwright (primary), with single-browser focus for speed and reliability.

## Current Architecture Decision (Updated 2025-08-28)

### Serial Execution Strategy
After extensive analysis, we have determined that our monolithic architecture with SQLite and EF Core is incompatible with parallel E2E test execution. We are adopting an optimized serial execution strategy.

**Rationale**:
- SQLite's single-writer limitation creates lock contention with multiple workers
- EF Core's DbContext cannot dynamically switch schemas per request
- Worker isolation requires either complete server duplication or complex routing
- The complexity cost of parallel execution outweighs the speed benefits

**Trade-offs Accepted**:
- Longer total test execution time (10 minutes vs theoretical 3 minutes parallel)
- Sequential rather than parallel test development
- Single browser testing by default (cross-browser only for critical paths)

## Test Architecture

### Server Management
- **Single shared server set**: One API server, one Angular server
- **Started once**: Global setup/teardown, not per test file
- **Lean configuration**: Minimal middleware, no hot reload, errors-only logging
- **Database**: SQLite file with delete-and-recreate cleanup strategy

### Test Categorization

#### Smoke Tests (@smoke)
- **Scope**: Critical user paths only
- **Target**: < 2 minutes
- **When**: Every commit, pre-push hooks
- **Examples**: Login, basic CRUD operations

#### Critical Tests (@critical) 
- **Scope**: Core business features
- **Target**: < 5 minutes
- **When**: Pull requests
- **Examples**: Complete workflows, permission checks

#### Extended Tests (@extended)
- **Scope**: Edge cases, comprehensive validation
- **Target**: < 10 minutes (full suite)
- **When**: Pre-merge, nightly builds
- **Examples**: Error handling, boundary conditions

#### Cross-Browser Tests
- **Scope**: Critical tests only
- **Browsers**: Chrome, Firefox, Safari/WebKit
- **Target**: < 15 minutes
- **When**: Release candidates, weekly

## Implementation Details

### Test Fixture Configuration
```typescript
// Serial execution with reliable cleanup
export const test = base.extend({
  // Database reset before each test
  autoCleanup: [async ({ request }, use) => {
    await request.post('/api/test/reset-database');
    await use();
  }, { auto: true }],
  
  // Single worker configuration
  workers: 1,
});
```

### Database Cleanup Strategy
```csharp
// Simple, reliable cleanup via file deletion
public async Task ResetDatabase()
{
    await _context.Database.CloseConnectionAsync();
    var dbPath = GetDatabasePath();
    
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
    }
    
    await _context.Database.EnsureCreatedAsync();
    await SeedMinimalData();
}
```

## Performance Optimizations

### Speed Improvements
1. **Server startup**: Once per test run (not per file)
2. **Browser reuse**: Single context across related tests
3. **Smart waits**: Event-based rather than time-based
4. **Batch operations**: Group API calls for test setup
5. **Minimal UI**: Disable animations, skip unnecessary resources

### Reliability Improvements
1. **Deterministic cleanup**: File deletion guarantees clean state
2. **No parallel conflicts**: Serial execution prevents race conditions
3. **Retry logic**: Handle transient SQLite locks
4. **Health checks**: Ensure servers ready before tests start

## CI/CD Pipeline

### Staged Execution
```yaml
stages:
  - smoke     # 2 min - fail fast
  - critical  # 5 min - PR validation  
  - full      # 10 min - merge validation
  - cross     # 15 min - nightly only
```

## Developer Workflow

### Local Development
```bash
npm test:smoke      # Quick feedback (2 min)
npm test:critical   # Before pushing (5 min)
npm test            # Full validation (10 min)
```

### Debugging
- Single worker makes reproduction reliable
- Can inspect SQLite file between tests
- Clear test categorization helps focus debugging

## Migration Path from Parallel Tests

1. **Week 1**: Fix database cleanup, implement serial execution
2. **Week 2**: Optimize test speed, categorize tests
3. **Ongoing**: Monitor and optimize slowest tests

## Success Metrics

- **Reliability**: 100% pass rate (no flaky tests)
- **Smoke Tests**: < 2 minutes
- **Full Suite**: < 10 minutes  
- **Maintenance**: < 2 hours/week on test infrastructure

## Future Considerations

If performance becomes unacceptable (> 15 min full suite):
1. Investigate PostgreSQL migration for schema-based isolation
2. Consider API-only tests for non-UI validations
3. Evaluate container-based test isolation
4. Reduce E2E coverage in favor of integration tests

## Best Practices

1. **Write fast tests**: Minimize UI interactions
2. **Use data builders**: Quick test data setup
3. **Avoid dependencies**: Tests should be independent
4. **Clean as you go**: Each test cleans up after itself
5. **Tag appropriately**: Accurate @smoke/@critical tags

## Example Test Structure

```typescript
test.describe('@smoke Authentication', () => {
  test('user can login', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[name="email"]', 'test@example.com');
    await page.fill('[name="password"]', 'password');
    await page.click('button[type="submit"]');
    
    await page.waitForURL('/dashboard');
    expect(page.url()).toContain('/dashboard');
  });
});
```

## Conclusion

Our E2E testing strategy prioritizes **reliability over speed** and **simplicity over complexity**. By accepting serial execution and optimizing within those constraints, we achieve predictable, maintainable test execution that provides confidence in our application's behavior.