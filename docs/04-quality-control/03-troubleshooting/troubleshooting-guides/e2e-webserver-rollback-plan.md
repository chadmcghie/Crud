# E2E WebServer Configuration Rollback Plan

## Overview
This document provides a rollback strategy if the new Playwright webServer configuration encounters issues.

## Current State (As of 2025-01-04)
- **New Configuration**: `playwright.config.webserver.ts` using Playwright's built-in webServer
- **Legacy Configuration**: `playwright.config.serial.ts` with custom `optimized-global-setup.ts`
- **Status**: Both configurations coexist, new one is recommended

## Rollback Triggers

Consider rollback if:
1. WebServer configuration fails consistently in CI (>3 consecutive failures)
2. Database locking issues persist despite unique file approach
3. Performance degrades significantly (>2x slower)
4. Server startup becomes unreliable

## Rollback Steps

### Quick Rollback (Immediate)
1. **Switch test command in CI**:
   ```yaml
   # Change from:
   npx playwright test --config=playwright.config.webserver.ts
   # To:
   npx playwright test --config=playwright.config.serial.ts
   ```

2. **Update package.json** default:
   ```json
   "test": "playwright test --config=playwright.config.serial.ts"
   ```

3. **Notify team** of temporary rollback

### Full Rollback (If Permanent)

1. **Revert GitHub Actions workflow**:
   ```bash
   git revert <commit-hash-of-webserver-workflow>
   ```

2. **Update documentation**:
   - Revert CLAUDE.md changes
   - Update README.md in test directory
   - Mark ADR-002 as "Rejected"

3. **Remove webServer files**:
   ```bash
   rm playwright.config.webserver.ts
   rm tests/setup/webserver-teardown.ts
   rm tests/fixtures/database-fixture.ts
   ```

4. **Restore serial config as default**:
   ```bash
   mv playwright.config.serial.ts playwright.config.ts
   ```

## Mitigation Strategies

Before rollback, try these fixes:

### Issue: Database Locking
**Fix**: Ensure TEST_RUN_ID is truly unique
```typescript
const testRunId = `${Date.now()}-${process.pid}-${Math.random()}`;
```

### Issue: Server Won't Start
**Fix**: Increase timeout and add retry logic
```typescript
webServer: {
  timeout: 180 * 1000, // 3 minutes
  reuseExistingServer: true,
}
```

### Issue: Port Conflicts
**Fix**: Use dynamic port allocation
```typescript
const apiPort = process.env.API_PORT || (5172 + Math.floor(Math.random() * 100));
```

### Issue: CI-Specific Failures
**Fix**: Add CI-specific configuration
```typescript
if (process.env.CI) {
  config.webServer.env.ASPNETCORE_URLS = 'http://0.0.0.0:5172';
}
```

## Monitoring Period

- **Week 1-2**: Daily monitoring of CI pipeline
- **Week 3-4**: Address any issues that arise
- **Month 2**: Decision point - commit or rollback
- **Month 3**: If successful, remove legacy configuration

## Success Criteria

Consider the migration successful if:
- ✅ 95% pass rate in CI over 2 weeks
- ✅ No database locking issues
- ✅ Test execution time comparable or better
- ✅ Simpler maintenance (less code)

## Rollback Decision Matrix

| Issue | Severity | Action |
|-------|----------|--------|
| Single test failure | Low | Debug and fix test |
| Intermittent failures (<10%) | Medium | Add retry logic |
| Consistent failures (>30%) | High | Quick rollback |
| Complete failure | Critical | Immediate rollback |

## Communication Plan

1. **If rolling back**:
   - Post in #dev-channel
   - Update PR with rollback reason
   - Create issue for investigation

2. **If keeping**:
   - Document any workarounds
   - Update team on benefits realized
   - Schedule legacy code removal

## Lessons Learned Log

### What Worked
- Playwright's webServer simplifies configuration
- Unique database files prevent locking
- Same config for local and CI

### What Didn't Work
- (To be filled based on experience)

### Improvements for Next Time
- (To be filled based on experience)

## Commands Reference

```bash
# Test with new configuration
npm run test:webserver

# Test with old configuration
npm run test:serial

# Emergency stop all servers
./kill-servers.ps1

# Clean up test databases
rm -f CrudTest_*.db*
```

## Contact

For issues or questions about rollback:
- Check docs/02-Architecture/Decisions/0002-E2E-Database-Performance-Optimization.md
- Review docs/03-Development/status/current-status.md for context
- Consult git history for original implementation