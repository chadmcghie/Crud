# E2E Tests for CRUD Application

This directory contains end-to-end tests for the CRUD application using Playwright.

## Test Configuration

Uses Playwright's built-in `webServer` feature for automatic server management.

**Benefits:**
- Automatic server lifecycle management
- Unique database per test run (prevents locking)
- No custom server management code
- Works reliably in CI/CD
- Simpler configuration

**How it works:**
- Playwright starts API and Angular servers automatically
- Each test run gets a unique SQLite database file
- Database is cleaned up after tests complete
- Servers are reused locally, fresh in CI

## Available Scripts

```bash
# Test execution
npm run test              # Run ALL E2E tests (~15-20 min)
npm run test:smoke        # Run only smoke tests (@smoke tag, ~2-3 min)
npm run test:critical     # Run critical tests (@critical tag, ~5-10 min)

# Utility commands
npm run report            # Show test report
npm run install-browsers  # Install Playwright browsers
npm run clean             # Clean test results
```

## Test Categories

Tests are tagged for selective execution:
- `@smoke` - Quick validation tests (~2-3 minutes)
- `@critical` - Important feature tests (~5-10 minutes)
- Untagged tests are included in the full suite (~15-20 minutes)

## Architecture Decision

See [ADR-002: E2E Testing Database Strategy](../../docs/Decisions/0002-E2E-Testing-Database-Strategy.md) for details on why we moved to Playwright's webServer configuration.

### Key Points:
- SQLite file locking issues in CI required a new approach
- Playwright's webServer eliminates 300+ lines of custom code
- Unique database files per test run prevent locking
- Same configuration works locally and in CI

## CI/CD Integration

The GitHub Actions workflow uses the webServer configuration:
- `.github/workflows/e2e-webserver.yml` - Main E2E test workflow
- Automatic server startup and shutdown
- Unique database per workflow run
- Test artifacts uploaded for debugging

## Troubleshooting

### Database Locking Issues
If you encounter "database is locked" errors:
1. Ensure you're using the webServer configuration
2. Check that TEST_RUN_ID is unique
3. Verify no other processes are using the database

### Server Port Conflicts
If ports 5172 (API) or 4200 (Angular) are in use:
1. Stop any running servers: `npm run servers:stop`
2. Check for processes: `lsof -i :5172` or `lsof -i :4200`
3. Kill processes if needed

### Test Failures in CI
Check the uploaded artifacts:
- Test results in HTML and JSON format
- Database files (if tests failed)
- Server logs (if available)

## Development Tips

1. **Use webServer config for reliability**: The webServer configuration is more reliable and simpler.

2. **Keep tests independent**: Each test should work in isolation with database reset.

3. **Use fixtures**: Leverage the `database-fixture.ts` for automatic cleanup.

4. **Monitor test duration**: Keep smoke tests under 3 minutes, critical under 10 minutes.

5. **Debug locally first**: Use `npx playwright test --headed` or `--debug` to see what's happening.

## Migration from Legacy Setup

If you have custom test configurations:
1. Use the default `playwright.config.ts` (includes webServer configuration)
2. Ensure all test commands include required environment variables
3. Database isolation is handled automatically with unique filenames
4. Playwright manages server lifecycle via built-in webServer feature

## Contributing

When adding new tests:
1. Tag appropriately (@smoke, @critical) if needed for selective execution
2. Ensure tests are independent
3. Use the webServer configuration
4. Add to appropriate spec file category
5. Keep tests focused and fast