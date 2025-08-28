import { test as base, TestInfo, APIRequestContext, Page } from '@playwright/test';
import { PersistentServerManager } from './persistent-server-manager';

export interface TestFixtures {
  apiContext: APIRequestContext;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
  serverInfo: { apiUrl: string; angularUrl: string; database: string };
}

// Test-scoped fixtures only - no worker fixtures needed since servers persist
export const test = base.extend<TestFixtures>({
  // Get server info (single instance for serial execution)
  serverInfo: async ({ }, use) => {
    console.log(`🔧 Getting server info...`);
    
    const manager = PersistentServerManager.getInstance();
    const info = await manager.ensureServers();
    
    await use(info);
  },

  apiUrl: async ({ serverInfo }, use) => {
    await use(serverInfo.apiUrl);
  },

  angularUrl: async ({ serverInfo }, use) => {
    await use(serverInfo.angularUrl);
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

  cleanDatabase: [async ({ apiContext }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for: ${testInfo.title}`);
    
    // For serial execution, we can clean the database more aggressively
    const manager = PersistentServerManager.getInstance();
    await manager.cleanDatabase();
    
    await use();
  }, { auto: true }],

  // Override page to use worker-specific Angular URL
  page: async ({ browser, angularUrl }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    
    // Update base URL for this worker
    page.setDefaultNavigationTimeout(45000);
    page.setDefaultTimeout(15000);
    
    // Navigate to worker-specific Angular URL
    await page.goto(angularUrl);
    
    await use(page);
    
    await context.close();
  },
});

export { expect } from '@playwright/test';