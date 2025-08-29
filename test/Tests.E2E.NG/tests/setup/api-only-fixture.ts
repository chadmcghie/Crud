import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { PersistentServerManager } from './persistent-server-manager';

export interface ApiOnlyFixtures {
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  cleanDatabase: void;
  serverInfo: { apiUrl: string; angularUrl: string; database: string };
}

// Simple API-only fixture that reuses servers
export const test = base.extend<ApiOnlyFixtures>({
  // Get server info (single instance for serial execution)
  serverInfo: async ({ }, use) => {
    console.log(`🔧 Getting server info...`);
    
    const manager = PersistentServerManager.getInstance();
    const info = await manager.ensureServers();
    
    await use(info);
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

  apiHelpers: async ({ apiContext }, use) => {
    const helpers = new ApiHelpers(apiContext, 0); // Single worker, always index 0
    await use(helpers);
  },

  cleanDatabase: [async ({ apiContext }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for: ${testInfo.title}`);
    
    // For serial execution, we can clean the database more aggressively
    const manager = PersistentServerManager.getInstance();
    await manager.cleanDatabase();
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';