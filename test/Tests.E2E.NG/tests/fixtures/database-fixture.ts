import { test as base } from '@playwright/test';

/**
 * Database Test Fixture
 * 
 * Provides automatic database reset functionality between tests
 * when using Playwright's webServer configuration.
 */

export const test = base.extend({
  // Automatically reset database before each test
  page: async ({ page, request }, use) => {
    // Reset database before test
    try {
      const response = await request.post('http://localhost:5172/api/database/reset', {
        headers: {
          'Content-Type': 'application/json',
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        },
        data: {
          preserveSchema: true,
          workerIndex: 0
        }
      });
      
      if (!response.ok()) {
        console.warn(`Database reset returned ${response.status()}`);
      }
    } catch (error) {
      console.warn('Could not reset database:', error);
      // Continue with test even if reset fails
    }
    
    // Use the page
    await use(page);
    
    // Cleanup after test if needed
    // (Database cleanup is handled by unique database files per run)
  },
});

export { expect } from '@playwright/test';