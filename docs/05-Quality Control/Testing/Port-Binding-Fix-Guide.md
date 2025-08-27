# Port Binding Fix Guide

**Date**: 2025-08-27  
**Status**: Resolved

## Problem Description

The E2E tests and CI/CD workflows were experiencing port binding conflicts where multiple API server instances were trying to bind to the same port (5172) instead of using their assigned worker-specific ports.

### Root Cause

The issue was caused by the `launchSettings.json` file in the API project containing hardcoded port configurations:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5172",
      // ...
    }
  }
}
```

When running `dotnet run`, the application was using these hardcoded launch profiles instead of respecting the `ASPNETCORE_URLS` environment variable set by the E2E test worker setup.

## Solution Implemented

### 1. Updated Worker Setup

Modified `test/Tests.E2E.NG/tests/setup/worker-setup.ts` to use the `--no-launch-profile` flag:

```typescript
this.apiProcess = spawn('dotnet', ['run', '--no-launch-profile'], {
  cwd: '../../src/Api',
  env: apiEnv,
  stdio: ['pipe', 'pipe', 'pipe']
});
```

This ensures the API server respects the `ASPNETCORE_URLS` environment variable instead of using launch profile settings.

### 2. Updated CORS Policy

Extended the CORS policy in `src/Api/Program.cs` to allow multiple Angular ports for parallel workers:

```csharp
policy.WithOrigins(
  "http://localhost:4200", "http://127.0.0.1:4200", "https://localhost:4200",
  "http://localhost:4210", "http://localhost:4220", "http://localhost:4230",
  "http://localhost:4240", "http://localhost:4250", "http://localhost:4260"
)
```

### 3. CI/CD Workflow Updates

Updated the existing GitHub Actions workflow (`/.github/workflows/pr-validation.yml`) that:

- Kills any existing processes on test ports before running tests
- Uses the API-only test configuration to avoid Angular port conflicts
- Properly handles the fixed port binding approach

## Port Assignment Strategy

The parallel test infrastructure now correctly assigns ports as follows:

| Worker | API Port | Angular Port | Database |
|--------|----------|--------------|----------|
| 0 | 5172 | 4200 | `/tmp/CrudTest_Worker0_{timestamp}.db` |
| 1 | 5173 | 4210 | `/tmp/CrudTest_Worker1_{timestamp}.db` |
| 2 | 5174 | 4220 | `/tmp/CrudTest_Worker2_{timestamp}.db` |
| 3 | 5175 | 4230 | `/tmp/CrudTest_Worker3_{timestamp}.db` |

## Verification

After implementing the fix:

✅ **Unit Tests**: 106/106 passing  
✅ **Integration Tests**: All passing  
✅ **E2E API Tests**: 54/54 passing  
✅ **Port Isolation**: Each worker uses dedicated ports
✅ **CI/CD Ready**: GitHub Actions workflow handles port conflicts

## Prevention

To prevent this issue in the future:

1. **Always use `--no-launch-profile`** when programmatically starting the API in test scenarios
2. **Document port ranges** used by different test configurations
3. **Include port cleanup** in CI/CD workflows before running tests
4. **Test locally** with multiple workers to ensure port isolation works

## Related Files

- `test/Tests.E2E.NG/tests/setup/worker-setup.ts` - Worker port assignment and server startup
- `src/Api/Program.cs` - CORS policy and server configuration
- `src/Api/Properties/launchSettings.json` - Launch profile settings (bypassed in tests)
- `.github/workflows/pr-validation.yml` - CI/CD pipeline with port handling
- `.github/workflows/codeql.yml` - Security analysis workflow
- `.github/dependabot.yml` - Dependency update automation

This fix ensures reliable parallel testing both locally and in CI/CD environments.