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
      console.log('📡 Sending database reset request...');
      const startTime = Date.now();
      
      const response = await apiContext.post('/api/database/reset', {
        data: { workerIndex: 0, preserveSchema: true },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        },
        timeout: 30000 // 30 second timeout to allow for detailed logging
      });
      
      const duration = Date.now() - startTime;
      console.log(`📡 Database reset response received in ${duration}ms`);
      
      if (!response.ok()) {
        console.warn(`Database reset failed: ${response.status()}`);
        const body = await response.text();
        console.warn(`Response body: ${body}`);
      } else {
        const body = await response.json();
        console.log(`✅ Database reset successful in ${body.Duration || duration}ms`);
      }
    } catch (error: any) {
      if (error.message?.includes('timeout')) {
        console.error('❌ Database reset TIMEOUT after 30 seconds!');
        console.error('This indicates the API is not responding to database reset requests');
      } else if (error.message?.includes('Request context disposed')) {
        console.error('❌ Request context was disposed during database reset');
        console.error('This may indicate timing issues with test cleanup');
      } else {
        console.warn('⚠️ Database cleanup error:', error.message || error);
      }
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';
