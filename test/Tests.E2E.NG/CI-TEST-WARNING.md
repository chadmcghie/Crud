# ⚠️ CRITICAL: E2E Test Configuration for CI ⚠️

## DO NOT CHANGE THE TEST COMMANDS

The `test:smoke`, `test:critical`, and `test:extended` scripts in package.json
are configured specifically for CI compatibility. They MUST use 
`playwright.config.webserver.ts`.

## History of This Issue
- Fixed multiple times, broken multiple times
- Each "simplification" breaks CI
- The complex commands are NOT a mistake
- See GitHub issue #79 for permanent solution

## The Rule
**NEVER** change these commands to the "simpler" form:
```json
// ❌ WRONG - BREAKS CI
"test:smoke": "playwright test --grep @smoke"

// ✅ CORRECT - WORKS IN CI  
"test:smoke": "npx cross-env TEST_CATEGORY=smoke ... playwright test --config=playwright.config.webserver.ts"
```

## Why It Keeps Breaking
Developers (and AI assistants) see the long command and think it can be
simplified. IT CANNOT. The complexity is there for a reason.

## Files That Should NOT Be Used for CI Tests
- `playwright.config.ts` - Has global-setup issues
- `tests/setup/global-setup.ts` - Manual server management fails in CI
- `playwright.config.serial.ts` - Also uses global-setup

## The Correct File
- `playwright.config.webserver.ts` - Uses Playwright's built-in server management

If you're reading this because tests are failing in CI, check if someone
"simplified" the test commands in package.json.