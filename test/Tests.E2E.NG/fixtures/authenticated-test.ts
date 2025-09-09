import { test as base } from '@playwright/test';
import { AuthHelper } from '../utils/auth.helper';

/**
 * Extended test fixture with authentication support
 */
export const test = base.extend<{
  authHelper: AuthHelper;
  authenticatedPage: void;
}>({
  authHelper: async ({ page }, use) => {
    const helper = new AuthHelper(page);
    await use(helper);
  },

  authenticatedPage: [async ({ page, authHelper }, use) => {
    // Setup: Login before test
    await authHelper.login();
    
    // Run test
    await use();
    
    // Cleanup: Logout after test (optional)
    // await authHelper.logout();
  }, { auto: true }] // auto: true means this runs automatically for tests using this fixture
});

export { expect } from '@playwright/test';