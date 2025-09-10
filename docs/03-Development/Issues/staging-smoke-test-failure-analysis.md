# Staging Smoke Test Failure - Root Cause Analysis

## The Real Problem

The smoke tests are failing because **Angular is never built or started** in the staging workflow before running the tests.

## Evidence from `deploy-staging.yml`

### What the workflow does:
1. **Line 51-62**: Builds .NET backend only
2. **Line 97-101**: Installs E2E test dependencies only
3. **Line 104-109**: Runs smoke tests

### What's missing:
- No `npm ci` in `src/Angular` directory
- No Angular build step
- No Angular server startup

## Why Tests Fail

The Playwright config (`playwright.config.ts`) has a webServer configuration that tries to start both servers:

```typescript
webServer: [
  {
    // API Server
    command: 'dotnet run --project ../../src/Api/Api.csproj',
    ...
  },
  {
    // Angular Server  
    command: process.env.CI ? 'npm run start:ci' : 'npm start',
    cwd: path.join(process.cwd(), '..', '..', 'src', 'Angular'),
    ...
  }
]
```

But the Angular server startup fails because:
1. Angular dependencies are never installed (`npm ci` is never run in `src/Angular`)
2. The `npm run start:ci` command fails due to missing node_modules

## The Fix

Add Angular setup before running smoke tests in `deploy-staging.yml`:

```yaml
# After line 81 (Setup Node.js), add:

- name: Install Angular dependencies
  working-directory: ./src/Angular
  run: npm ci
  
# Optionally build Angular for faster startup:
- name: Build Angular
  working-directory: ./src/Angular  
  run: npm run build
```

## Alternative Solutions

### Option 1: Install Angular deps (Minimal fix)
```yaml
- name: Install Angular dependencies
  working-directory: ./src/Angular
  run: npm ci
```

### Option 2: Use pre-built artifacts
Modify the Playwright config to use already-built artifacts instead of starting from source.

### Option 3: Create a CI-specific Playwright config
Create `playwright.ci.config.ts` that doesn't use webServer and expects servers to be already running.

## Why This Wasn't Obvious

1. The error message just shows "TimeoutError waiting for selector" 
2. No clear indication that Angular server failed to start
3. Playwright's webServer startup errors are suppressed (`stdout: 'ignore', stderr: 'ignore'`)

## Immediate Action

The quickest fix is to add Angular dependency installation before running the smoke tests. This will allow the Playwright webServer configuration to successfully start the Angular dev server.