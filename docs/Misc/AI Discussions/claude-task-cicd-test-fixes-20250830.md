# CI/CD Test Infrastructure Fixes

## TL;DR
Fixed multiple critical issues in the CI/CD pipeline including missing JWT configuration, incorrect job dependencies, and potential runaway test execution that could consume hours of CI time.

## Key Points

### Problems Identified
- **Integration tests failing with HTTP 500 errors** - JWT configuration missing in Testing environment
- **E2E tests running despite integration test failures** - Incorrect job dependencies with `always()` condition
- **Potential runaway test execution** - No circuit breaker, could run 500+ failing tests at 30s each (4+ hours!)
- **Missing error details in CI logs** - Test runner not configured for console output
- **Angular server startup timeouts** - 90 seconds insufficient in CI environment

### Solutions Implemented

#### 1. JWT Configuration Fix
- Added JWT settings to `appsettings.Testing.json`
- Moved configuration file to correct location (`src/Api/`)
- Ensured file copying in Api.csproj with `CopyToOutputDirectory=Always`
- Added artifact handling in workflow to include appsettings files

#### 2. Test Job Dependencies
- Fixed E2E job to properly depend on backend-integration-tests
- Removed `always()` condition that was causing tests to run even after failures
- E2E tests now only run if ALL dependencies succeed

#### 3. Circuit Breaker Implementation
- Added `maxFailures` configuration to Playwright configs
- Stops after 5-10 failures in CI to prevent runaway execution
- Reduced retries from 2 to 1 for faster failure detection
- Reduced test timeout to 30 seconds for quicker feedback

#### 4. Enhanced Error Logging
- Added console logger with detailed verbosity to test commands
- Implemented TestLogCapture infrastructure for server-side error logging
- Updated GlobalExceptionHandlingMiddleware to provide full stack traces in Testing environment
- Added error logging methods to IntegrationTestBase

#### 5. Angular CI Improvements
- Created `start:ci` script with proper host binding (0.0.0.0)
- Increased startup timeout to 3 minutes in CI environments
- Fixed permission issues for .NET binary execution

## Technical Details

### File Changes
- **Workflow**: `.github/workflows/pr-validation.yml`
  - Added permissions configuration
  - Fixed artifact handling
  - Added console logging to test runners
  - Fixed job dependencies

- **Configuration**: `src/Api/appsettings.Testing.json`
  - Added JWT configuration
  - Enhanced logging levels
  - Moved to correct location

- **Test Infrastructure**:
  - `SqliteTestWebApplicationFactory.cs` - Removed incorrect content root configuration
  - `IntegrationTestBase.cs` - Added error logging methods
  - `TestLogCapture.cs` - Created for capturing server logs

- **E2E Tests**: 
  - `playwright.config.*.ts` - Added circuit breaker configuration
  - `optimized-global-setup.ts` - Fixed Angular startup for CI

## Decisions/Resolutions

1. **Use in-memory configuration for tests** - Considered but opted for file-based configuration for consistency
2. **Circuit breaker limits** - Set to 5 failures for fast config, 10 for standard configs
3. **Console logging verbosity** - `detailed` for integration tests, `normal` for unit tests
4. **No automatic commits** - User explicitly requested no commits without permission

## Next Steps

1. Push the changes to trigger CI/CD pipeline
2. Monitor test output for actual error messages with new logging
3. Verify JWT configuration is being loaded correctly
4. Confirm E2E tests skip when integration tests fail
5. Validate circuit breaker prevents runaway test execution

## Lessons Learned

- Always include circuit breakers in test configurations to prevent CI resource exhaustion
- Job dependencies must not use `always()` unless intentionally running regardless of failures
- Console output is crucial for debugging CI failures - TRX files alone are insufficient
- Configuration file locations matter - tests need access to environment-specific settings
- Path calculations in test factories can be fragile - prefer simpler approaches

## Commands for Testing Locally

```bash
# Run integration tests with detailed output
dotnet test test/Tests.Integration.Backend/Tests.Integration.Backend.csproj --logger "console;verbosity=detailed"

# Run E2E tests with circuit breaker
cd test/Tests.E2E.NG
npm run test:fast
```

---
*Generated: 2025-08-30*
*Context: Fixing CI/CD pipeline test failures in Crud repository*