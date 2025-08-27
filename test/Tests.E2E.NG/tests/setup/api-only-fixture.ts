import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';

export interface ApiOnlyFixtures {
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  cleanDatabase: void;
  workerIndex: number;
}

// Simple API-only fixture that assumes a single server is running
export const test = base.extend<ApiOnlyFixtures>({
  workerIndex: async ({}, use, testInfo) => {
    await use(testInfo.workerIndex);
  },

  apiContext: async ({ playwright }, use) => {
    const context = await playwright.request.newContext({
      baseURL: 'http://localhost:5172',
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });
    await use(context);
    await context.dispose();
  },

  apiHelpers: async ({ apiContext, workerIndex }, use) => {
    const helpers = new ApiHelpers(apiContext, workerIndex);
    await use(helpers);
  },

  cleanDatabase: [async ({ apiContext, workerIndex }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for worker ${workerIndex}, test: ${testInfo.title}`);
    
    // Pre-test database cleanup using the shared database service
    try {
      const response = await apiContext.post('/api/database/reset', {
        data: { workerIndex }
      });
      
      if (!response.ok()) {
        console.warn(`⚠️ Database reset failed for worker ${workerIndex}: ${response.status()}`);
      } else {
        console.log(`✅ Database reset completed for worker ${workerIndex}`);
      }
    } catch (error) {
      console.error(`❌ Database cleanup failed for worker ${workerIndex}:`, error);
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';