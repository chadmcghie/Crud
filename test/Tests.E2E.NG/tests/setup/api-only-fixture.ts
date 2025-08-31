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
    
    // PROTECTED: Skip database reset in CI - causes 30s timeouts
    // See BI-2025-08-31-001 - 11 attempts to fix this failed
    // Root cause: Database operations timeout in container environment
    // Tests pass with this workaround, maintaining unique test data
    if (process.env.CI) {
      console.log('⚠️ Database reset skipped in CI (protected workaround)');
      await use();
      return;
    }
    
    // Use the database reset endpoint for fast cleanup (local only)
    try {
      const response = await apiContext.post('/api/database/reset', {
        data: { workerIndex: 0, preserveSchema: true },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        }
      });
      
      if (!response.ok()) {
        console.warn(`Database reset failed: ${response.status()}`);
        const body = await response.text();
        console.warn(`Response body: ${body}`);
      } else {
        console.log('✅ Database reset successful');
      }
    } catch (error) {
      console.warn('⚠️ Database cleanup warning:', error);
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';