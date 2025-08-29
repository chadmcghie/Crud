# E2E Tests for CRUD Application

## Overview
End-to-end tests for the Angular frontend and .NET API using Playwright with **serial execution** strategy (ADR-001).

## Quick Start

```bash
# Install dependencies
npm install
npx playwright install

# Run all tests (default serial mode)
npm test

# Run smoke tests only
npm run test:smoke

# Run with UI
npm run test:ui
```

## Test Strategy

### Core Principle: Serial Execution
- **ALL tests run serially** with `workers: 1`
- **NO parallel execution** or worker complexity
- **Single shared database** with resets between tests
- See `E2E-TEST-STRATEGY.md` for detailed documentation

### Test Configurations

| Config | Purpose | Browsers | Use Case |
|--------|---------|----------|----------|
| `test` | Default serial execution | Chromium | Local development |
| `test:fast` | Quick feedback | Chromium only | Development iterations |
| `test:ci` | Full validation | All 3 browsers | CI/CD pipeline |
| `test:api-only` | API tests only | N/A | API validation |
| `test:integration` | Full workflows | Chromium | Integration testing |

### Server Management
- Servers start **once** at test suite start
- Servers stop **once** at test suite end
- **Never restarted** during test execution
- Database resets between tests, not servers

## Common Commands

```bash
# Run specific test suites
npm run test:smoke        # Smoke tests only
npm run test:critical      # Critical tests
npm run test:extended      # Extended test suite

# Development helpers
npm run test:headed        # Run with browser visible
npm run test:debug         # Debug mode
npm run test:ui           # Interactive UI mode

# CI/CD
npm run test:ci           # Full CI suite (3 browsers)
npm run test:fast         # Fast single-browser run

# Utilities
npm run report            # Show test report
npm run clean             # Clean test artifacts
npm run install-browsers  # Install Playwright browsers
```

## Troubleshooting

### Tests Timing Out
- Check API is running on port 5172
- Check Angular is running on port 4200
- Kill any stale processes: `lsof -i :4200 -i :5172`

### Database Issues
- Database resets use raw SQL for speed (2-3ms)
- SQLite requires separate DELETE statements
- Connection pool issues fixed with ExecuteSqlRawAsync

### Performance
- Smoke tests: <2 minutes
- Full suite: <10 minutes
- Database reset: 2-3ms (not 30s)

## Project Structure

```
Tests.E2E.NG/
├── tests/
│   ├── smoke.spec.ts           # Quick validation tests
│   ├── serial-example.spec.ts  # Serial execution examples
│   ├── angular-ui/             # UI-focused tests
│   ├── api/                    # API-focused tests
│   ├── integration/            # Full workflow tests
│   ├── fixtures/               # Test fixtures
│   ├── helpers/                # Test utilities
│   └── setup/                  # Global setup/teardown
├── playwright.config.ts         # Main configuration
├── playwright.config.*.ts       # Variant configurations
├── E2E-TEST-STRATEGY.md        # Detailed strategy docs
└── package.json                 # Scripts and dependencies
```

## Important Notes

### DO NOT
- ❌ Add parallel execution or worker complexity
- ❌ Restart servers between tests
- ❌ Use ExecuteDeleteAsync for database cleanup
- ❌ Manually manage database connections
- ❌ Add workerIndex parameters

### DO
- ✅ Run tests serially (workers: 1)
- ✅ Use raw SQL for database cleanup
- ✅ Let global setup manage servers
- ✅ Follow ADR-001 principles
- ✅ Check E2E-TEST-STRATEGY.md for details

## Related Documentation
- [E2E Test Strategy](./E2E-TEST-STRATEGY.md) - Detailed test strategy and troubleshooting
- [ADR-001](../../.agent-os/recaps/2025-08-29-e2e-test-simplification.md) - Serial execution decision