# ⚠️ CRITICAL: E2E Test Configuration for CI ⚠️

## DO NOT CHANGE THE TEST COMMANDS

The `test:smoke`, `test:critical`, `test:extended`, and `test:webserver` scripts in package.json
are configured specifically for CI compatibility. They MUST include all environment variables.

## History of This Issue
- Fixed multiple times, broken multiple times
- Each "simplification" breaks CI
- The complex commands are NOT a mistake
- Environment variables are REQUIRED for proper server configuration

## The Rule
**NEVER** change these commands to the "simpler" form:
```json
// ❌ WRONG - BREAKS CI
"test:smoke": "playwright test --grep @smoke"

// ✅ CORRECT - WORKS IN CI  
"test:smoke": "npx cross-env TEST_CATEGORY=smoke API_PORT=5172 ANGULAR_PORT=4200 DATABASE_PATH=test.db API_URL=http://localhost:5172 ANGULAR_URL=http://localhost:4200 playwright test"
```

## Why It Keeps Breaking
Developers (and AI assistants) see the long command and think it can be
simplified. IT CANNOT. The environment variables are essential for:
- Proper port configuration
- Database path isolation
- API/Angular URL configuration
- CI/CD compatibility

## Current Configuration
As of September 2025, all tests use the default `playwright.config.ts` which includes:
- Built-in webServer configuration for automatic server management
- Unique database files per test run to prevent locking
- Serial execution (workers: 1) for SQLite compatibility
- Proper CI/CD environment detection

## The Test Commands
All test commands follow the same pattern with environment variables:
- `test`: Basic playwright test
- `test:webserver`: Full test suite with all environment variables
- `test:smoke`: Quick 2-minute smoke tests (@smoke tag)
- `test:critical`: 5-minute critical path tests (@critical tag)
- `test:extended`: 10-minute extended test suite (@extended tag)

If you're reading this because tests are failing in CI, check if someone
"simplified" the test commands in package.json by removing environment variables.