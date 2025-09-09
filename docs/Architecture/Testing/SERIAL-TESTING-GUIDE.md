# Serial E2E Testing Guide

## Quick Start

```bash
# Run all tests serially (default)
npm test

# Run only smoke tests (2 min)
npm run test:smoke

# Run critical tests (5 min)
npm run test:critical

# Run extended tests (10 min)
npm run test:extended

# Run with UI mode for debugging
npm run test:ui

# Start servers manually (they stay running)
npm run servers:start

# Stop servers when done
npm run servers:stop
```

## Why Serial Testing?

We've adopted serial test execution based on architectural constraints:

1. **SQLite Database Limitations** - SQLite doesn't handle concurrent writes well
2. **Entity Framework Core** - EF Core with SQLite has known issues with parallel access
3. **Reliability Over Speed** - 100% pass rate is better than flaky fast tests
4. **Simplicity** - Less complexity = easier maintenance

See `/docs/02-Architecture/ADR-001-Serial-E2E-Testing.md` for the full decision record.

## Test Categories

Tests are organized into three categories using tags:

### @smoke (2 minutes)
- Application health checks
- Core navigation works
- API endpoints respond
- Basic CRUD operations

### @critical (5 minutes)  
- Essential user workflows
- Create, edit, delete operations
- Form validation
- Search and filtering

### @extended (10 minutes)
- Edge cases
- Error handling
- Performance scenarios
- Data integrity checks

## File Structure

```
test/Tests.E2E.NG/
├── playwright.config.serial.ts      # Main serial configuration
├── tests/
│   ├── fixtures/
│   │   └── serial-test-fixture.ts   # Custom test fixture with DB cleanup
│   ├── setup/
│   │   ├── serial-global-setup.ts   # Starts shared servers
│   │   ├── serial-global-teardown.ts # Cleanup
│   │   ├── database-utils.ts        # SQLite management
│   │   └── port-utils.ts            # Port management
│   ├── smoke.spec.ts                # Smoke test suite
│   ├── serial-example.spec.ts       # Example patterns
│   └── [other test files]
└── scripts/
    ├── start-test-servers.ps1       # Start servers manually
    └── stop-test-servers.ps1         # Stop servers

```

## Writing Tests

### Use the Serial Test Fixture

```typescript
import { test, expect, tagTest } from './fixtures/serial-test-fixture';

test.describe('Feature Tests', () => {
  test(tagTest('should do something', 'smoke'), async ({ page }) => {
    // Test automatically gets fresh database
    // Servers are already running
  });
});
```

### Tag Your Tests

Always tag tests for proper categorization:

```typescript
// Smoke test - runs in smoke suite
test('@smoke should load homepage', async ({ page }) => {
  // Quick validation
});

// Critical test - important user flow
test('@critical should complete checkout', async ({ page }) => {
  // Essential functionality
});

// Extended test - comprehensive scenario
test('@extended should handle concurrent users', async ({ page }) => {
  // Edge cases and complex scenarios
});
```

### Database Is Reset Automatically

The serial test fixture automatically resets the database before each test:

```typescript
test('creates test data', async ({ page, apiUrl }) => {
  // Database is clean at start
  const response = await page.request.post(`${apiUrl}/api/people`, {
    data: { fullName: 'Test Person' }
  });
  // Data exists only for this test
});

test('next test has clean database', async ({ page, apiUrl }) => {
  // Previous test's data is gone
  const response = await page.request.get(`${apiUrl}/api/people`);
  const data = await response.json();
  // data will be empty (unless seed data exists)
});
```

## Server Management

### Automatic (Recommended)

Tests automatically start servers if not running:

```bash
npm test  # Servers start automatically
```

### Manual Control

For faster subsequent runs, keep servers running:

```bash
# Terminal 1 - Start and keep servers running
npm run servers:start

# Terminal 2 - Run tests (reuses servers)
npm test
npm test  # Faster - servers already running
npm test  # Still fast

# When done - stop servers
npm run servers:stop
```

## CI/CD Configuration

```yaml
# Example GitHub Actions
- name: Run E2E Tests
  run: |
    cd test/Tests.E2E.NG
    npm ci
    npm run test:smoke     # Quick validation (2 min)
    npm run test:critical  # Important flows (5 min)
    # npm run test:extended # Only on main branch (10 min)
```

## Troubleshooting

### Tests Still Flaky?

1. Check database cleanup in test output
2. Ensure only one worker: `workers: 1` in config
3. Look for "Database reset" messages in logs
4. Verify servers aren't restarting between tests

### Tests Too Slow?

1. Run smoke tests first: `npm run test:smoke`
2. Keep servers running between test runs
3. Use `test.only` during development
4. Consider moving UI tests to API tests where possible

### Port Conflicts?

```bash
# Windows - Find what's using port
netstat -ano | findstr :5172
netstat -ano | findstr :4200

# Kill specific process
taskkill /F /PID [process_id]

# Or use our cleanup script
npm run servers:stop
```

### Database Locked?

The framework has retry logic, but if persistent:

1. Stop all servers: `npm run servers:stop`
2. Delete temp databases manually:
   - Look for `CrudTest_*.db` files in your system's temp directory
   - Temp directory location varies by OS (use `echo $TEMP` or `echo $TMPDIR` to find it)
3. Restart tests

## Performance Tips

1. **Keep Servers Running** - Start once, run many times
2. **Use Test Categories** - Don't run all tests every time
3. **Optimize Selectors** - Use data-testid attributes
4. **Minimize Waits** - Use Playwright's auto-waiting
5. **API Over UI** - Test logic via API, UI for user flows

## Migration Checklist

When migrating existing tests to serial:

- [ ] Change config to use `playwright.config.serial.ts`
- [ ] Update imports to use `serial-test-fixture`
- [ ] Add appropriate tags (@smoke, @critical, @extended)
- [ ] Remove parallel test assumptions
- [ ] Remove manual database cleanup (automatic now)
- [ ] Update CI pipeline to run serially

## Success Metrics

✅ Your tests are successful when:
- 10 consecutive runs pass without failure
- Smoke tests complete in < 2 minutes
- Full suite completes in < 10 minutes
- No database cleanup errors
- Team prefers the new approach

## More Information

- [Migration Summary](/docs/05-Quality Control/Testing/E2E-Test-Migration-Summary.md)
- [Architecture Decision Record](/docs/02-Architecture/ADR-001-Serial-E2E-Testing.md)
- [Testing Strategy](/docs/05-Quality Control/Testing/1-Testing Strategy.md)