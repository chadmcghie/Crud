# ⚠️ CRITICAL: E2E Test Configuration for CI ⚠️

## DO NOT CHANGE THE TEST COMMANDS

The `test:smoke` and `test:critical` scripts in package.json are configured specifically for CI compatibility. They MUST include all environment variables.

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
"test:smoke": "npx cross-env CI=true TEST_CATEGORY=smoke API_PORT=5172 ANGULAR_PORT=4200 DATABASE_PATH=test.db API_URL=http://localhost:5172 ANGULAR_URL=http://localhost:4200 BYPASS_AUTHORIZATION_FOR_E2E=true E2E_TEST_MODE=true playwright test"
```

## Why It Keeps Breaking
Developers (and AI assistants) see the long command and think it can be simplified. IT CANNOT. The environment variables are essential for:
- Proper port configuration
- Database path isolation
- API/Angular URL configuration
- CI/CD compatibility
- Test mode authorization bypass

## Current Configuration
As of October 2025, all tests use the default `playwright.config.ts` which includes:
- Built-in webServer configuration for automatic server management
- Unique database files per test run to prevent locking
- Serial execution (workers: 1) for SQLite compatibility
- Proper CI/CD environment detection

## The Test Commands
Three test levels are available:
- `test`: Runs ALL E2E tests (~15-20 min, no filter)
- `test:smoke`: Quick smoke tests (@smoke tag, ~2-3 min)
- `test:critical`: Critical path tests (@critical tag, ~5-10 min)

If you're reading this because tests are failing in CI, check if someone "simplified" the test commands in package.json by removing environment variables.