# E2E Tests for CRUD Application

This directory contains end-to-end tests for the CRUD application using Playwright with a simplified serial execution approach.

## Test Configuration

The tests use a single, simplified Playwright configuration (`playwright.config.ts`) that implements:

- **Serial Execution**: Single worker with `fullyParallel: false` for SQLite compatibility
- **No Retries**: Tests should be reliable and fail fast if there are issues
- **Simple Server Management**: Uses global setup/teardown for server lifecycle
- **Test Categorization**: Supports @smoke, @critical, and @extended tags

## Available Scripts

```bash
# Main test commands
npm test                    # Run all tests (default serial execution)
npm run test:smoke         # Run only smoke tests (@smoke tag)
npm run test:critical      # Run critical tests (@critical tag)  
npm run test:extended      # Run extended tests (@extended tag)
npm run test:ci            # Run tests in CI mode

# Development commands
npm run test:headed        # Run tests with browser visible
npm run test:ui            # Open Playwright UI mode
npm run test:debug         # Run tests in debug mode

# Utility commands
npm run report             # Show test report
npm run install-browsers   # Install Playwright browsers
npm run clean              # Clean test artifacts
```

## Test Categories

Tests are tagged for selective execution:
- `@smoke` - Quick validation tests (< 2 minutes)
- `@critical` - Important feature tests (< 5 minutes)
- `@extended` - Comprehensive tests (< 10 minutes)

## Architecture Decision

This simplified approach is based on [ADR-001: Serial E2E Testing](../../docs/Decisions/0001-Serial-E2E-Testing.md).

### Key Principles:
- **Serial Execution**: Single worker prevents SQLite database conflicts
- **No Parallel Complexity**: Eliminates race conditions and locking issues
- **Simple Server Management**: Basic startup/shutdown without persistence logic
- **File-based Database Cleanup**: Simple delete/recreate pattern for SQLite
- **100% Test Reliability**: No flaky tests through deterministic execution

## Performance Targets

- **Test Reliability**: 100% (no flaky tests)
- **Smoke Tests**: < 2 minutes
- **Critical Tests**: < 5 minutes  
- **Full Suite**: < 10 minutes
- **Server Startup**: < 30 seconds
- **Database Reset**: < 1 second per file

## CI/CD Integration

The GitHub Actions workflow uses the simplified configuration:
- Automatic server startup via global setup
- Serial test execution for reliability
- Test artifacts uploaded for debugging
- Environment-specific configurations

## Test Structure

```
tests/
├── integration/           # Complete workflow tests
│   ├── full-workflow.spec.ts
│   └── complete-infrastructure-flow.spec.ts
├── api/                  # API-only tests
│   ├── people-api.spec.ts
│   ├── roles-api.spec.ts
│   └── walls-api.spec.ts
├── angular-ui/           # UI interaction tests
│   ├── app-navigation.spec.ts
│   ├── people.spec.ts
│   └── roles.spec.ts
├── setup/               # Test infrastructure
│   ├── global-setup.ts
│   ├── global-teardown.ts
│   └── database-utils.ts
└── helpers/             # Test utilities
    ├── api-helpers.ts
    ├── page-helpers.ts
    └── test-data.ts
```

## Troubleshooting

### Database Issues
If you encounter database-related errors:
1. Check that the database file is not locked by another process
2. Verify the global setup/teardown is working correctly
3. Ensure proper cleanup between test files

### Server Port Conflicts
If ports 5172 (API) or 4200 (Angular) are in use:
1. Check for running processes: `lsof -i :5172` or `lsof -i :4200`
2. Kill any conflicting processes
3. Restart the test suite

### Test Failures
1. Run tests with `--headed` to see browser behavior
2. Check test artifacts in `./test-results/`
3. Use `--debug` mode for step-by-step debugging
4. Verify server health endpoints are accessible

## Development Tips

1. **Use Test Categories**: Tag tests appropriately for selective execution
2. **Keep Tests Independent**: Each test should work in isolation
3. **Use Helpers**: Leverage `api-helpers.ts` and `page-helpers.ts` for common operations
4. **Monitor Performance**: Keep tests within target time limits
5. **Debug Locally**: Use `--headed` and `--debug` modes for development

## Contributing

When adding new tests:
1. Tag appropriately (@smoke, @critical, @extended)
2. Ensure tests are independent and can run in any order
3. Use the provided helper classes for common operations
4. Add to appropriate spec file category
5. Keep tests focused and fast
6. Follow the serial execution pattern (no parallel assumptions)