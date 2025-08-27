# E2E Tests for Angular & API

Comprehensive end-to-end tests for the Angular frontend and .NET API backend.

## Quick Start

```bash
# Install dependencies
npm install

# Run all tests (recommended for first run)
npm run test:stable

# Run tests with UI
npm run test:ui
```

## Test Commands

### Basic Commands
- `npm test` - Run all tests with default configuration
- `npm run test:headed` - Run tests with browser window visible
- `npm run test:ui` - Open Playwright UI mode
- `npm run test:debug` - Run tests in debug mode

### Optimized Commands
- `npm run test:stable` - **Recommended**: Pre-builds Angular and runs tests with single worker for maximum stability
- `npm run test:fast` - Fast configuration with reduced workers
- `npm run test:single` - Run tests with single worker (most stable)
- `npm run prebuild:test` - Pre-build Angular then run tests

### Specific Test Suites
- `npm run test:api-only` - Run only API tests (no UI)
- `npm run test:integration` - Run integration tests
- `npm run test:parallel` - Run tests with maximum parallelization

### CI/CD
- `npm run test:ci` - Optimized configuration for CI environments
- `npm run prebuild` - Pre-build Angular application for faster startup

### Utilities
- `npm run report` - Show HTML test report
- `npm run clean` - Clean test results and reports
- `npm run install-browsers` - Install Playwright browsers

## Performance Optimizations

### 1. Pre-built Angular (Fastest Startup)
The tests can use a pre-built Angular application for much faster startup:

```bash
# Build Angular once
npm run prebuild

# Run tests using the pre-built version
npm test
```

### 2. Worker Queue System
Tests use a queue system to prevent port conflicts when running multiple workers in parallel.

### 3. Static File Server
When Angular is pre-built, tests use a lightweight static file server instead of the Angular dev server.

## Configuration Files

- `playwright.config.ts` - Default configuration
- `playwright.config.fast.ts` - Optimized for speed with single worker
- `playwright.config.ci.ts` - CI/CD optimized with sharding support
- `playwright.config.api-only.ts` - API tests only (no browser)
- `playwright.config.parallel.ts` - Maximum parallelization
- `playwright.config.local.ts` - Local development configuration

## Troubleshooting

### Angular Server Timeout
If you encounter Angular server startup timeouts:

1. **Use single worker mode**:
   ```bash
   npm run test:single
   ```

2. **Pre-build Angular**:
   ```bash
   npm run test:stable
   ```

3. **Increase memory**:
   ```bash
   NODE_OPTIONS="--max-old-space-size=8192" npm test
   ```

### Port Conflicts
If you see "address already in use" errors:

1. **Check for running processes**:
   ```bash
   lsof -i :4200,4210,5172,5173
   ```

2. **Kill stuck processes**:
   ```bash
   kill -9 <PID>
   ```

3. **Use single worker mode**:
   ```bash
   npm run test:single
   ```

### Test Failures
For intermittent test failures:

1. **Run with retries**:
   ```bash
   npx playwright test --retries=3
   ```

2. **Use stable configuration**:
   ```bash
   npm run test:stable
   ```

3. **Debug specific test**:
   ```bash
   npx playwright test --debug path/to/test.spec.ts
   ```

## CI/CD Integration

### GitHub Actions
The repository includes a GitHub Actions workflow (`.github/workflows/e2e-tests.yml`) that:
- Runs tests in parallel using sharding
- Caches Angular builds for faster CI
- Uploads test results and reports
- Comments on PRs with test results

### Running in CI
```yaml
- name: Run E2E Tests
  run: npm run test:ci
```

## Architecture

### Test Structure
```
tests/
├── angular-ui/     # UI tests using Playwright
├── api/           # API tests
├── integration/   # Full workflow tests
├── helpers/       # Test utilities
├── fixtures/      # Test data and fixtures
└── setup/         # Test configuration and setup
```

### Worker Isolation
Each test worker runs with:
- Dedicated API server port (5172 + workerIndex)
- Dedicated Angular server port (4200 + workerIndex * 10)
- Isolated SQLite database
- Automatic cleanup between tests

### Key Components
1. **WorkerServerManager** - Manages server lifecycle per worker
2. **WorkerStartupQueue** - Prevents port conflicts during startup
3. **ApiHelpers** - API testing utilities with retry logic
4. **PageHelpers** - UI testing utilities
5. **Test Fixtures** - Ensures proper server startup before tests

## Best Practices

1. **Always use custom test fixtures** in UI tests:
   ```typescript
   import { test, expect } from '../setup/test-fixture';
   ```

2. **Clean up data between tests**:
   ```typescript
   test.beforeEach(async ({ apiContext }) => {
     await apiHelpers.cleanupAll();
   });
   ```

3. **Use retry logic for API calls**:
   ```typescript
   await apiHelpers.retryOperation(() => apiHelpers.createPerson(data));
   ```

4. **Wait for specific elements instead of arbitrary timeouts**:
   ```typescript
   await page.waitForSelector('button:has-text("Save")');
   ```

## Development Tips

1. **Fast iteration**: Use `npm run test:ui` for interactive development
2. **Debugging**: Use `npm run test:debug` with breakpoints
3. **Specific tests**: Use `--grep` to run specific tests:
   ```bash
   npm test -- --grep="should create"
   ```
4. **Single file**: Test a specific file:
   ```bash
   npx playwright test tests/api/people-api.spec.ts
   ```

## Performance Metrics

Typical execution times:
- Cold start (no pre-build): ~30-60s per worker
- With pre-build: ~5-10s per worker
- API tests only: ~2-5s per test
- UI tests: ~5-15s per test

## Contributing

When adding new tests:
1. Use the appropriate test fixture (`test-fixture.ts` for UI, `api-only-fixture.ts` for API)
2. Follow existing patterns for data cleanup
3. Add meaningful test descriptions
4. Use data-testid attributes for reliable element selection
5. Consider adding to both API and UI test suites