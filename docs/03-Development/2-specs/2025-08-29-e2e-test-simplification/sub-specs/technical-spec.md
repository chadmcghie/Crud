# Technical Specification

This is the technical specification for the spec detailed in [spec.md](../spec.md)

## Technical Requirements

### Configuration Layer Changes

1. **Main Playwright Configuration (`playwright.config.ts`)**
   - Set `workers: 1` for single worker execution
   - Set `fullyParallel: false` to enforce serial execution
   - Remove `retries` configuration (set to 0)
   - Use single browser project (Chromium only by default)
   - Configure proper globalSetup and globalTeardown paths

2. **Remove Alternative Configurations**
   - Delete or consolidate multiple config files (parallel, improved, etc.)
   - Keep only essential configs: main, serial, CI, and api-only
   - Ensure all configs inherit from base serial settings

### Server Management Simplification

1. **Global Setup (`global-setup.ts`)**
   - Simple server startup: API on port 5172, Angular on port 4200
   - Use `spawn` with proper environment variables
   - Wait for health checks using simple fetch loops
   - Store PIDs in environment variables for teardown
   - No lock files or persistence logic needed

2. **Global Teardown (`global-teardown.ts`)**
   - Kill processes by PID
   - Clean up SQLite database file
   - Simple port cleanup as fallback
   - No complex state management

3. **Remove Complex Components**
   - Delete `PersistentServerManager` class entirely
   - Remove lock file management
   - Eliminate server reuse logic between test runs
   - Remove unnecessary port scanning complexity

### Database Management

1. **SQLite File Operations**
   - Simple delete and recreate pattern
   - Database path: `{temp}/CrudTest_Serial_{timestamp}.db`
   - Reset before each test file (not each test)
   - No connection pooling or complex cleanup

2. **Test Fixtures**
   - Simple beforeAll hook for database reset
   - No per-test cleanup (only per-file)
   - Remove database size monitoring unless debugging

### CI/CD Pipeline Updates

1. **GitHub Actions Workflow**
   - Remove manual server startup steps
   - Let Playwright globalSetup handle servers
   - Simplify to: checkout → setup → install → run tests
   - Use proper Playwright config for CI environment

2. **Environment Variables**
   - `CI=true` for CI detection
   - `ASPNETCORE_ENVIRONMENT=Testing`
   - `DatabaseProvider=SQLite`
   - Remove complex server management variables

### Test Organization

1. **Test Categorization**
   - Implement @smoke, @critical, @extended tags
   - Use grep patterns in config for filtering
   - Smoke tests: 2 minutes target
   - Full suite: 10 minutes target

2. **Package.json Scripts**
   - `npm test`: Run default serial tests
   - `npm run test:smoke`: Quick validation
   - `npm run test:ci`: CI-specific configuration
   - Remove complex script variations

## Performance Criteria

- **Test Reliability**: 100% (no flaky tests)
- **Smoke Tests**: < 2 minutes
- **Critical Tests**: < 5 minutes  
- **Full Suite**: < 10 minutes
- **Server Startup**: < 30 seconds
- **Database Reset**: < 1 second per file

## Migration Path

1. **Phase 1**: Update main playwright.config.ts to serial settings
2. **Phase 2**: Simplify global setup/teardown
3. **Phase 3**: Remove PersistentServerManager and complex logic
4. **Phase 4**: Update CI pipeline to use globalSetup
5. **Phase 5**: Clean up unused configurations and scripts

## Testing the Changes

1. Run `npm test` locally - should complete without errors
2. Run `npm run test:smoke` - should complete in < 2 minutes
3. Push to CI - should use globalSetup and pass reliably
4. Verify no server processes left running after tests
5. Confirm SQLite files are cleaned up properly