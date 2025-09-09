import { test as base } from '@playwright/test';

/**
 * Test fixture that enables E2E bypass mode
 * Allows tests to run without full authentication setup
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    // Enable E2E mode before tests
    await page.addInitScript(() => {
      localStorage.setItem('e2e-test-mode', 'active');
    });
    
    await use(page);
    
    // Clean up after tests
    await page.evaluate(() => {
      localStorage.removeItem('e2e-test-mode');
    });
  }
});

export { expect } from '@playwright/test';