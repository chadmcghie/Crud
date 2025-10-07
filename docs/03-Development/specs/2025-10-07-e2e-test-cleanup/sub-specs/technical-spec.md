# Technical Specification

This is the technical specification for the spec detailed in @docs/03-Development/specs/2025-10-07-e2e-test-cleanup/spec.md

## Technical Requirements

### Phase 1: Clean Angular Production Code

#### 1.1 auth.service.ts Cleanup (src/Angular/src/app/auth.service.ts)

**Remove E2E Detection Logic:**
- Delete `isE2ETestEnvironment()` method (lines 93-169, ~77 lines)
- Remove complex user agent detection, port checking, localStorage flag checks
- Remove Playwright-specific browser detection logic

**Remove E2E Auto-Authentication:**
- Delete E2E mode check and mock user creation in constructor (lines 49-72, ~24 lines)
- Remove localStorage.setItem('e2e-test-mode', 'active') calls
- Remove mock user object creation for E2E tests
- Remove all E2E-related console.log debug statements (~10 lines)

**Clean Constructor:**
- Keep only normal authentication flow
- Remove E2E detection branching logic
- Maintain existing token validation and refresh scheduling

**Total Lines Removed:** ~134 lines

#### 1.2 app.ts Cleanup (src/Angular/src/app/app.ts)

**Remove E2E Attributes:**
- Line 18: Remove `[attr.data-e2e-ready]="isE2EReady"` binding from template
- Line 21: Remove `[attr.data-e2e-nav]="true"` attribute

**Remove E2E Properties:**
- Line 250: Delete `isE2EReady = false;` property declaration
- Lines 271-273: Remove E2E ready state management in auth subscription

**Remove Debug Code:**
- Lines 276-282: Delete debug setTimeout with DOM inspection logic
- Remove E2E-related console.log statements

**Clean Template:**
- Keep navigation structure intact
- Maintain RouterLink functionality
- Preserve user authentication display

**Total Lines Removed:** ~15 lines

#### 1.3 auth.guard.ts Cleanup (src/Angular/src/app/auth.guard.ts)

**Remove TestAuthService Dependency:**
- Line 15: Remove `import { TestAuthService } from './test-auth.service';`
- Lines 17-21: Delete E2E bypass logic in `checkAuth()` function
- Lines 38, 48, 58, 68, 77, 78: Remove `testAuthService` parameters and injections

**Simplify Guard Logic:**
- Keep only AuthService and Router dependencies
- Maintain returnUrl functionality
- Preserve all guard function variants (canActivate, canActivateChild, canLoad, canMatch)

**Updated checkAuth Function:**
```typescript
function checkAuth(authService: AuthService, router: Router, returnUrl?: string): boolean | UrlTree {
  if (authService.isAuthenticated()) {
    return true;
  }

  const queryParams = returnUrl ? { returnUrl } : undefined;
  return router.createUrlTree(['/login'], { queryParams });
}
```

**Total Lines Removed:** ~5 lines

#### 1.4 test-auth.service.ts Relocation/Deletion

**Decision Tree:**
1. Check if service is used by Angular unit tests (*.spec.ts files in src/Angular)
2. IF used by unit tests:
   - Move to `src/Angular/src/testing/test-auth.service.ts`
   - Update import paths in unit tests
3. ELSE:
   - Delete `src/Angular/src/app/test-auth.service.ts` entirely

**Rationale:** Test utilities should live in testing directories, not production app directories.

### Phase 2: Implement Test-Specific Infrastructure

#### 2.1 Create Test Authentication Helper

**File:** `test/Tests.E2E.NG/helpers/test-auth-setup.ts`

**Implementation:**
```typescript
import { Page } from '@playwright/test';

export class TestAuthSetup {
  /**
   * Setup mock authentication for E2E tests using Playwright APIs
   * This replaces the E2E detection logic previously in Angular code
   */
  static async setupMockAuth(page: Page): Promise<void> {
    // Use Playwright's addInitScript to inject mock auth before Angular loads
    await page.addInitScript(() => {
      // Mock localStorage tokens
      localStorage.setItem('access_token', 'mock-e2e-token');
      localStorage.setItem('user', JSON.stringify({
        id: 'e2e-test-user',
        email: 'e2e@test.com',
        roles: ['User', 'Admin']
      }));
    });
  }

  /**
   * Setup API route interception to bypass auth endpoints
   */
  static async setupAuthRouteInterception(page: Page, apiUrl: string): Promise<void> {
    // Intercept auth validation endpoints
    await page.route(`${apiUrl}/api/auth/validate`, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ valid: true })
      });
    });

    // Intercept token refresh endpoints
    await page.route(`${apiUrl}/api/auth/refresh`, async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'mock-refreshed-token',
          refreshToken: 'mock-refresh-token'
        })
      });
    });
  }

  /**
   * Clear mock authentication
   */
  static async clearMockAuth(page: Page): Promise<void> {
    await page.evaluate(() => {
      localStorage.removeItem('access_token');
      localStorage.removeItem('user');
      sessionStorage.clear();
    });
  }
}
```

#### 2.2 Update Test Fixtures

**File:** `test/Tests.E2E.NG/tests/fixtures/simple-test-fixture.ts`

**Changes:**
- Import TestAuthSetup helper
- Call `TestAuthSetup.setupMockAuth(page)` in beforeEach hook
- Optionally call `setupAuthRouteInterception()` if needed
- Call `clearMockAuth()` in afterEach hook for cleanup

**Example Integration:**
```typescript
import { TestAuthSetup } from '../../helpers/test-auth-setup';

test.beforeEach(async ({ page }) => {
  await TestAuthSetup.setupMockAuth(page);
  await TestAuthSetup.setupAuthRouteInterception(page, 'http://localhost:5172');
});

test.afterEach(async ({ page }) => {
  await TestAuthSetup.clearMockAuth(page);
});
```

### Phase 3: Enhance page-helpers.ts

**File:** `test/Tests.E2E.NG/tests/helpers/page-helpers.ts`

**Add New Event-Driven Wait Methods:**

```typescript
/**
 * Wait for Angular navigation to complete
 * Replaces arbitrary timeout waits after navigation actions
 */
async waitForNavigationComplete(): Promise<void> {
  await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });

  // Wait for Angular to be ready
  await this.page.waitForFunction(() => {
    return typeof (window as any).ng !== 'undefined';
  }, { timeout: 10000 });

  // Wait for network to settle (optional, can be flaky)
  await this.page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {
    // Ignore networkidle failures, not critical
  });

  // CI-specific additional stability wait
  if (isCI) {
    await this.page.waitForTimeout(500);
  }
}

/**
 * Wait for data to load from API
 * Replaces timeouts between rapid API operations
 */
async waitForDataLoad(apiPattern: string = '/api/'): Promise<void> {
  await this.page.waitForResponse(
    response => response.url().includes(apiPattern) && response.ok(),
    { timeout: 10000 }
  );

  // Brief wait for Angular change detection
  await this.page.waitForTimeout(isCI ? 500 : 200);
}

/**
 * Wait for component to be ready and interactive
 * Replaces initial page load timeouts
 */
async waitForComponentReady(selector: string, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout || (isCI ? 15000 : 10000);

  // Wait for component to exist
  await this.page.locator(selector).waitFor({
    state: 'visible',
    timeout
  });

  // Wait for component to be interactive (not disabled/loading)
  await this.page.waitForFunction(
    (sel) => {
      const element = document.querySelector(sel);
      if (!element) return false;

      // Check if element is fully rendered and interactive
      const style = window.getComputedStyle(element);
      const isVisible = style.display !== 'none' && style.visibility !== 'hidden';
      const notLoading = !element.hasAttribute('aria-busy') ||
                        element.getAttribute('aria-busy') === 'false';

      return isVisible && notLoading;
    },
    selector,
    { timeout: 5000 }
  );
}

/**
 * Wait for form submission to complete
 * Replaces timeouts after form submits
 */
async waitForFormSubmission(expectedUrl?: string | RegExp): Promise<void> {
  // Wait for URL change if expected URL provided
  if (expectedUrl) {
    await this.page.waitForURL(expectedUrl, { timeout: 15000 });
  }

  // Wait for navigation to complete
  await this.waitForNavigationComplete();

  // Wait for any success messages or UI updates
  await this.page.waitForFunction(() => {
    const loadingIndicators = document.querySelectorAll('.loading, .spinner, [aria-busy="true"]');
    return loadingIndicators.length === 0;
  }, { timeout: 5000 }).catch(() => {
    // No loading indicators found, that's fine
  });
}
```

### Phase 4: Replace Timer-Based Waits

#### 4.1 full-workflow.spec.ts (5 timers → event-driven)

**Line 168:** `await pageHelpers.page.waitForTimeout(1000);`
- **Replace with:** `await pageHelpers.waitForDataLoad();`
- **Context:** After updating person via UI, wait for backend to reflect changes

**Lines 185, 189:** `await pageHelpers.page.waitForTimeout(100/500);`
- **Replace with:** `await pageHelpers.waitForDataLoad();`
- **Context:** Between rapid role creates to ensure database commits

**Line 239:** `await pageHelpers.page.waitForTimeout(100);`
- **Replace with:** `await pageHelpers.waitForDataLoad();`
- **Context:** Between sequential person creates

**Line 281:** `await pageHelpers.page.waitForTimeout(300);`
- **Replace with:** `await pageHelpers.waitForNavigationComplete();`
- **Context:** After navigation to roles list

#### 4.2 angular-ui/people.spec.ts (4 timers → existing helpers)

**Lines 175, 213, 257, 283:** `await page.waitForTimeout(1000);`
- **Replace with:** Use `pageHelpers.clickRefreshButton()` which already implements proper waits
- **Context:** All four are after refresh operations, helper already handles this correctly

#### 4.3 smoke.spec.ts (2 timers → component ready waits)

**Line 52:** `await page.waitForTimeout(2000);`
- **Replace with:** `await pageHelpers.waitForComponentReady('nav a[routerLink="/people-list"]');`
- **Context:** Waiting for auth state to stabilize after app load

**Line 104:** `await page.waitForTimeout(1000);`
- **Replace with:** `await pageHelpers.waitForNavigationComplete();`
- **Context:** After navigating to people list

#### 4.4 user-journeys/complete-user-workflows.spec.ts (1 timer)

**Line 27:** `await page.waitForTimeout(500);`
- **Replace with:** `await pageHelpers.waitForComponentReady('app-people-list button:has-text("Add New Person")');`
- **Context:** Waiting for component to stabilize after navigation

#### 4.5 reliability-scenarios.spec.ts (1 timer)

**Line 100:** `await page.waitForTimeout(1000);`
- **Replace with:** `await pageHelpers.waitForNavigationComplete();`
- **Context:** After navigating to invalid route

#### 4.6 password-reset.spec.ts (1 timer)

**Line 28:** `await page.waitForTimeout(1000);`
- **Replace with:** `await pageHelpers.waitForDataLoad('/api/auth/');`
- **Context:** Avoid rate limiting between API calls

### Integration Points

**With Existing Code:**
- page-helpers.ts already has retry logic and CI detection (isCI variable)
- Maintain existing `switchToPeopleTab()` and `switchToRolesTab()` patterns
- Preserve `clickRefreshButton()` implementation which already waits properly

**With Playwright Config:**
- No changes needed to playwright.config.ts
- webServer configuration remains unchanged
- Test categorization (@smoke, @critical, @extended) preserved

**With GitHub Actions CI:**
- CI environment variable already used throughout page-helpers
- Longer timeouts automatically applied in CI environments
- No workflow file changes required

### Performance Criteria

**Test Execution Time:**
- Local execution: Should remain similar or improve (±10%)
- CI execution: Should improve due to more reliable waits (reduce retries)

**Reliability Metrics:**
- Target: 0 failures due to timing issues in 10 consecutive CI runs
- Flake rate: Reduce from current ~5% to <1%

**Code Quality:**
- Zero `waitForTimeout()` calls in E2E tests (except in page-helpers for CI stability)
- Zero E2E-specific code in src/Angular/src/app/ directory
- All waits must be event-driven with explicit timeout values

## External Dependencies

No new external dependencies required. This refactoring uses existing Playwright APIs and Angular testing capabilities.
