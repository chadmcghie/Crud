# E2E Test Fixing Log

**Project**: Crud Application E2E Tests  
**Started**: August 2025  
**Status**: In Progress  

## Overview
We've been working on fixing failing E2E tests in the Playwright test suite. The tests use complex worker isolation with database cleanup strategies.

---

## Progress Summary

### Initial State
- **33 failing tests** (out of ~321 total)
- Main issues: Test isolation, data conflicts, double-prefixing, UI timing issues

### Current State  
- **~1-2 failing tests remaining** (significant progress!)
- Most critical issues resolved

---

## What We've Attempted

### ✅ **SUCCESSFUL FIXES**

#### 1. **Fixed Double-Prefixing Issue** 
- **Problem**: `ApiHelpers` was adding prefixes even when explicit names were provided
- **Root Cause**: `generateTestRole({ name: 'API Role' })` → API would create `"W1_T123_Role_API Role"`
- **Solution**: Added logic to detect explicit test names and skip prefixing
- **Result**: Fixed 3 parallel test failures across browsers

#### 2. **Fixed Test Data Generation Logic**
- **Problem**: `generateTestRole()` and `generateTestPerson()` applying worker prefixes incorrectly
- **Solution**: Modified functions to use explicit names exactly as provided
- **Code Location**: `test/Tests.E2E.NG/tests/helpers/test-data.ts`
- **Result**: Names now match expectations in tests

#### 3. **Fixed UI Delete Method Issues**
- **Problem**: Delete methods had inconsistent wait strategies, invalid selectors
- **Solution**: Improved `deletePerson()` and `deleteRole()` methods with better waits
- **Code Location**: `test/Tests.E2E.NG/tests/helpers/page-helpers.ts`
- **Result**: UI deletion tests now stable

#### 4. **Fixed Role Assignment Test**
- **Problem**: Using invalid `:near()` selector for checkboxes
- **Solution**: Changed to use proper `input[id="role-${roleId}"]` selectors
- **Code Location**: `test/Tests.E2E.NG/tests/angular-ui/people.spec.ts`
- **Result**: Role assignment tests working

#### 5. **Improved Test Isolation**
- **Problem**: UI tests not passing worker index to ApiHelpers
- **Solution**: Updated all UI test beforeEach hooks to pass `testInfo.workerIndex`
- **Result**: Better worker isolation, reduced data conflicts

#### 6. **Enhanced Cleanup Logic**
- **Problem**: Cleanup not recognizing various test data patterns
- **Solution**: Improved pattern matching in `cleanupAll()` method
- **Result**: Better cleanup of test data between runs

### ⚠️ **PARTIALLY SUCCESSFUL**

#### 1. **Description Prefixing Fix**
- **Problem**: Parallel tests failing on description comparison
- **Attempted**: Added `hasExplicitDescription` logic to skip "Worker:" prefixing
- **Status**: Fixed for single worker, may still have issues with multiple workers
- **Next**: Need to verify with multi-worker test run

#### 2. **Browser Refresh Timeout**
- **Problem**: Firefox timing out on `page.waitForLoadState('networkidle')`
- **Attempted**: Added fallback logic with shorter timeouts (8s → 5s)
- **Status**: Reduced timeout impact, but may still have occasional failures
- **Next**: Test with Firefox specifically

### ❌ **UNSUCCESSFUL ATTEMPTS**

#### 1. **Initial Cleanup Strategy**
- **Attempted**: Simple cleanup without understanding double-prefixing root cause
- **Why Failed**: Didn't address the core issue in ApiHelpers
- **Lesson**: Always trace issues to root cause before applying fixes

#### 2. **Complex Selector Fixes**
- **Attempted**: Using `:near()` and other non-standard selectors
- **Why Failed**: These selectors aren't reliable across browsers
- **Lesson**: Stick to standard CSS selectors with proper IDs/classes

#### 3. **Running Tests from Wrong Directory**
- **Attempted**: Running Playwright from project root instead of test directory
- **Why Failed**: Playwright picks up Angular unit tests instead of E2E tests
- **Lesson**: Always run Playwright commands from the correct test directory
- **Fix**: Must be in `test/Tests.E2E.NG/` directory

---

## Current Issues

### 🔄 **REMAINING FAILURES** (1-2 tests)

1. **Parallel Test Description Issue** (if multi-worker fails)
   - Test: `roles-api-parallel.spec.ts:9:7`
   - Issue: Description still getting "Worker:" prefix in multi-worker scenarios
   - Status: Fix applied, needs verification

2. **Browser Refresh Timeout** (Firefox-specific)
   - Test: `full-workflow.spec.ts:291:7`
   - Issue: Firefox timeout on page reload
   - Status: Timeout reduced, needs verification

---

## Key Learnings

### 🎯 **Root Cause Analysis is Critical**
- Don't fix symptoms, fix causes
- The double-prefixing issue was the root of many failures
- Tracing data flow from generation → API → UI revealed the real problem

### 🔧 **Test Isolation is Complex**
- Worker-based isolation with prefixing is powerful but fragile
- Multiple systems (test data generation, API helpers, cleanup) must work together
- Small changes can have cascading effects

### ⏱️ **Incremental Testing Saves Time**
- Running isolated failing tests (`--grep`) is much faster than full suite
- 30 seconds vs 20+ minutes makes iteration much more efficient

### 🌐 **Browser Differences Matter**
- Firefox handles page reloads differently than Chrome/Safari
- Always test fixes across all target browsers

---

## Next Steps

1. **Verify Multi-Worker Parallel Tests**
   ```bash
   npx playwright test --grep "should create role in isolated worker environment" --workers=3
   ```

2. **Test Firefox Browser Refresh**
   ```bash
   npx playwright test --grep "should handle browser refresh correctly" --project=firefox
   ```

3. **Run Full Suite to Confirm**
   ```bash
   npm run test
   ```

4. **Document Final State**
   - Update this log with final results
   - Create summary of remaining known issues (if any)

---

## Commands Reference

### Useful Test Commands
```bash
# Run only failing tests (fast iteration)
npx playwright test --grep "test name pattern"

# Run with specific number of workers
npx playwright test --workers=3

# Run specific browser
npx playwright test --project=firefox

# Run full suite
npm run test

# Show HTML report
npx playwright show-report
```

### Key Files Modified
- `test/Tests.E2E.NG/tests/helpers/api-helpers.ts` - Core API helper logic
- `test/Tests.E2E.NG/tests/helpers/test-data.ts` - Test data generation
- `test/Tests.E2E.NG/tests/helpers/page-helpers.ts` - UI interaction helpers
- `test/Tests.E2E.NG/tests/angular-ui/*.spec.ts` - UI test specs
- `test/Tests.E2E.NG/tests/integration/full-workflow.spec.ts` - Integration tests

---

*Last Updated: August 2025*  
*Next Update: After verifying remaining fixes*
