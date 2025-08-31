import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';

export interface ApiOnlyFixtures {
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  apiUrl: string;
  cleanDatabase: void;
}

// API-only test fixtures using environment variables from global setup
export const test = base.extend<ApiOnlyFixtures>({
  apiUrl: async ({ }, use) => {
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    await use(apiUrl);
  },

  apiContext: async ({ playwright, apiUrl }, use) => {
    const context = await playwright.request.newContext({
      baseURL: apiUrl,
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });
    await use(context);
    await context.dispose();
  },

  apiHelpers: async ({ apiContext }, use) => {
    const helpers = new ApiHelpers(apiContext, 0); // Single worker, always index 0
    await use(helpers);
  },

  cleanDatabase: [async ({ apiContext }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for: ${testInfo.title}`);
    
    // Use the database reset endpoint for fast cleanup
    try {
      const response = await apiContext.post('/api/database/reset', {
        data: { workerIndex: 0, preserveSchema: true },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        }
      });
      
      if (!response.ok()) {
        console.warn(`Database reset failed: ${response.status()}`);
      }
    } catch (error) {
      console.warn('⚠️ Database cleanup warning:', error);
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';