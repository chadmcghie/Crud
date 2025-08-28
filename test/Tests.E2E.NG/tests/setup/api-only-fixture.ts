import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { PersistentServerManager } from './persistent-server-manager';

export interface ApiOnlyFixtures {
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  cleanDatabase: void;
  workerIndex: number;
  parallelIndex: number;
  serverInfo: { apiUrl: string; angularUrl: string; database: string };
}

// Simple API-only fixture that reuses servers
export const test = base.extend<ApiOnlyFixtures>({
  // Get server info for this parallel worker
  serverInfo: async ({ }, use, workerInfo) => {
    const parallelIndex = workerInfo.parallelIndex;
    console.log(`🔧 Getting servers for parallel ${parallelIndex} (worker ${workerInfo.workerIndex})...`);
    
    const manager = new PersistentServerManager(parallelIndex);
    const info = await manager.ensureServers();
    
    await use(info);
  },
  
  parallelIndex: async ({ }, use, workerInfo) => {
    await use(workerInfo.parallelIndex);
  },
  
  workerIndex: async ({}, use, testInfo) => {
    await use(testInfo.workerIndex);
  },

  apiContext: async ({ playwright, serverInfo }, use) => {
    const context = await playwright.request.newContext({
      baseURL: serverInfo.apiUrl,
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

  cleanDatabase: [async ({ apiContext, parallelIndex }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for parallel ${parallelIndex}, test: ${testInfo.title}`);
    
    // Pre-test database cleanup using the shared database service
    try {
      const response = await apiContext.post('/api/database/reset', {
        data: { parallelIndex }
      });
      
      if (!response.ok()) {
        console.warn(`⚠️ Database reset failed for parallel ${parallelIndex}: ${response.status()}`);
      } else {
        console.log(`✅ Database reset completed for parallel ${parallelIndex}`);
      }
    } catch (error) {
      console.error(`❌ Database cleanup failed for parallel ${parallelIndex}:`, error);
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';