import { test as base, TestInfo, APIRequestContext, Page } from '@playwright/test';
import { PersistentServerManager } from './persistent-server-manager';

export interface TestFixtures {
  parallelIndex: number;
  apiContext: APIRequestContext;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
  workerIndex: number;  // Keep for backward compatibility
  serverInfo: { apiUrl: string; angularUrl: string; database: string };
}

// Test-scoped fixtures only - no worker fixtures needed since servers persist
export const test = base.extend<TestFixtures>({
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
  
  workerIndex: async ({ }, use, testInfo) => {
    // Provide workerIndex for backward compatibility
    await use(testInfo.workerIndex);
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

  cleanDatabase: [async ({ apiContext, parallelIndex }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for parallel ${parallelIndex}, test: ${testInfo.title}`);
    
    // Pre-test database cleanup
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