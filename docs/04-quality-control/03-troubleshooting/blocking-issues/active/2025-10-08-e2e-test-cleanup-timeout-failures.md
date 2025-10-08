---
id: BI-2025-10-08-001
status: active
category: test|functionality|configuration
severity: critical
created: 2025-10-08 10:00
resolved:
spec: all-future-specs
task: E2E test infrastructure stabilization
---

# E2E Test Cleanup and Timeout Failures

## Problem Statement
E2E tests are experiencing critical failures due to two interconnected issues:
1. Rapid sequential requests failing with timeouts, particularly following cleanup operations
2. Database cleanup not behaving consistently - tests expect clean databases but soft-delete functionality creates inconsistent state

## Symptoms
- Timeout errors during rapid test execution (5000ms timeout exceeded)
- Tests fail when run in sequence but may pass individually
- Database status endpoint reports entities exist after cleanup (including soft-deleted records)
- Cleanup verification shows data persists: "CLEANUP FAILED! Database still contains data"
- Tests inconsistently handle soft-delete state (IsDeleted flag)
- API under load warnings during test cleanup operations
- Intermittent failures that vary between local and CI environments

## Impact
- All E2E test development blocked
- Cannot reliably validate new features
- CI/CD pipeline instability
- Developer productivity severely impacted (2 weeks of troubleshooting)
- Cannot progress on any future specifications
- Test suite reliability compromised

## Root Cause Analysis (Five Whys)
1. Why are tests timing out during rapid execution?
   Answer: The API becomes unresponsive after cleanup operations

2. Why does the API become unresponsive after cleanup?
   Answer: Database cleanup operations are not completing synchronously, leaving the system in transitional state

3. Why are cleanup operations not completing synchronously?
   Answer: Soft-delete functionality creates complex state where entities exist but are marked as deleted

4. Why does soft-delete create complex state issues?
   Answer: Tests were designed for hard deletes but production uses soft deletes for audit trails

5. Why is there a mismatch between test expectations and production behavior?
   Answer: **ROOT CAUSE**: Test infrastructure was not properly architected to handle the production soft-delete pattern from the beginning

## Attempted Solutions

### Attempt 1: [2025-09-26 - 2025-10-01]
**Approach**: Removed test detection code from production Angular code, implemented event-driven waits
**Result**: Partial improvement but timeouts persisted during cleanup operations
**Files Modified**:
- src/Angular/src/app/test-auth.service.ts (removed)
- test/Tests.E2E.NG/tests/helpers/page-helpers.ts (enhanced)
- test/Tests.E2E.NG/tests/helpers/page-helpers-waits.spec.ts (created)
**Key Learning**: Event-driven waits improve reliability but don't address underlying cleanup issues

### Attempt 2: [2025-10-01 - 2025-10-05]
**Approach**: Enhanced DatabaseTestService with IgnoreQueryFilters for soft-delete handling
**Result**: Better visibility into database state but cleanup still fails intermittently
**Files Modified**:
- src/Infrastructure/Services/DatabaseTestService.cs (enhanced with retry logic)
- test/Tests.E2E.NG/tests/setup/test-fixture.ts (added verification with retries)
**Key Learning**: Soft-deleted entities persist and affect subsequent test runs despite cleanup attempts

### Attempt 3: [2025-10-05 - 2025-10-08]
**Approach**: Implemented multiple retry mechanisms, increased timeouts, added manual fallback cleanup
**Result**: Reduced failure rate but rapid sequential tests still timeout, cleanup verification still reports data
**Files Modified**:
- test/Tests.E2E.NG/tests/setup/test-fixture.ts (manual cleanup fallback added)
- test/Tests.E2E.NG/playwright.config.ts (timeout adjustments)
- src/Api/appsettings.Testing.json (configuration tuning)
**Key Learning**: The problem is architectural - soft-delete pattern fundamentally conflicts with test isolation requirements

### Attempt 4: [2025-10-08] - AWAITING CI VALIDATION
**Hypothesis**: Smoke tests passing (65/65) suggests cleanup IS working. Investigation revealed the real problem was exponential backoff retry delays causing 10+ second waits.
**Root Cause Found**: `PollyPolicies.GetDatabaseRetryPolicy()` uses exponential backoff (2^1=2s, 2^2=4s, 2^3=8s = 14s total) when SQLite encounters "busy" or "locked" errors during rapid operations. This is appropriate for production but unacceptable for tests.
**Approach**:
1. Modified `PollyPolicies.GetDatabaseRetryPolicy()` to detect Testing environment (`E2E_TEST_MODE=true`)
2. In test mode: Use faster retry intervals (100ms, 200ms, 300ms, 400ms, 500ms = 1.5s max)
3. In production: Keep exponential backoff (2s, 4s, 8s = 14s max)
**Result**: All 6 tests in full-workflow.spec.ts now PASS locally (previously 1 failing)
**Files Modified**:
- src/Infrastructure/Resilience/PollyPolicies.cs (lines 69-102): Added environment-aware retry timing
**Local Testing**: ✅ PASSING (6/6 tests in full-workflow.spec.ts)
**CI Testing**: ⏳ REQUIRED BEFORE MARKING RESOLVED

**⚠️ CRITICAL**: This fix MUST be validated in CI before marking as resolved. Local tests passing does NOT guarantee CI success, as these failures typically occur on slower CI infrastructure.

**CI Validation Checklist**:
- [ ] Push changes to feature branch (including workflow updates)
- [ ] Trigger manual E2E test run on CI:
  - Go to: GitHub Actions → "Manual E2E Tests" → "Run workflow"
  - Set branch to: `fix/e2e-test-cleanup`
  - Set test_file to: `tests/integration/full-workflow.spec.ts`
  - Click "Run workflow"
- [ ] Verify full-workflow.spec.ts passes in CI (all 6 tests)
- [ ] Run again with test_file empty to verify no regression in full suite
- [ ] If all CI tests pass, move issue to resolved/
- [ ] If CI fails, document new findings and continue troubleshooting

**How to Run Specific Test File in CI**:
```bash
# Manual trigger via GitHub UI:
# 1. Go to: Actions → Manual E2E Tests → Run workflow
# 2. Branch: fix/e2e-test-cleanup
# 3. Test file: tests/integration/full-workflow.spec.ts
# 4. Click Run workflow

# Or via GitHub CLI:
gh workflow run manual-e2e-tests.yml \
  --ref fix/e2e-test-cleanup \
  -f branch=fix/e2e-test-cleanup \
  -f test_file=tests/integration/full-workflow.spec.ts
```

**Key Learning**: Always investigate WHY something is slow before increasing timeouts. The 10-second timeout was masking a 14-second retry policy that was inappropriate for tests.

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: src/Infrastructure/Services/DatabaseTestService.cs - Lines: 45-85 - Change: Added IgnoreQueryFilters() for accurate entity counting - Reason: Required for visibility into soft-deleted records
- [x] File: test/Tests.E2E.NG/tests/helpers/page-helpers.ts - Lines: 200-388 - Change: Event-driven wait methods - Reason: Eliminates flaky timer-based waits
- [x] File: test/Tests.E2E.NG/tests/setup/test-fixture.ts - Lines: 106-150 - Change: Database state verification with retries - Reason: Provides visibility into cleanup failures
- [x] File: src/Api/appsettings.Testing.json - Lines: ALL - Change: Test-specific configuration - Reason: Optimizes for test execution environment
- [x] File: test/Tests.E2E.NG/tests/helpers/page-helpers-waits.spec.ts - Lines: ALL - Change: Comprehensive wait pattern tests - Reason: Documents and validates proper wait strategies

## Current Workaround
- Run tests individually or in very small batches with delays between executions
- Use `npm run test:serial` with single worker to minimize concurrency
- Manually verify and clean database between test runs if needed
- Skip tests that are known to be flaky in CI

## Next Steps
- [ ] Investigate implementing test-specific database instances with true hard deletes
- [ ] Consider separate test database schema without soft-delete triggers
- [ ] Evaluate database transaction rollback strategy for test isolation
- [ ] Research database snapshot/restore approach for instant cleanup
- [ ] Consider implementing test-specific API endpoints that bypass soft-delete
- [ ] Explore using in-memory database for E2E tests to eliminate persistence issues
- [ ] Document clear guidelines for handling soft-delete in test scenarios

## Related Issues
- Link to related blocking issue: BI-2025-09-25-001 (Test ecosystem instability)
- Link to GitHub issue/PR: #280 (E2E test infrastructure cleanup)
- Link to spec task: 2025-10-07-e2e-test-cleanup specification