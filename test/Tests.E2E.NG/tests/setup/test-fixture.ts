import { test as base, TestInfo, APIRequestContext, Page } from '@playwright/test';
import { WorkerServerManager } from './worker-setup';

export interface TestFixtures {
  workerSetup: WorkerServerManager;
  apiContext: APIRequestContext;
  cleanDatabase: void;
  workerIndex: number;
  apiUrl: string;
  angularUrl: string;
}

export interface WorkerFixtures {
  workerServerManager: WorkerServerManager;
}

// Worker-scoped fixture for server management
export const test = base.extend<TestFixtures, WorkerFixtures>({
  // Worker-scoped fixture - one per worker
  workerServerManager: [async ({ }, use, workerInfo) => {
    console.log(`🔧 Setting up worker ${workerInfo.workerIndex}...`);
    
    const serverManager = new WorkerServerManager(workerInfo);
    
    // Start servers for this worker
    await serverManager.startServers();
    
    // Set environment variables for this worker
    process.env.CURRENT_WORKER_INDEX = workerInfo.workerIndex.toString();
    process.env.CURRENT_WORKER_DATABASE = serverManager.getWorkerDatabase();
    process.env.CURRENT_WORKER_API_URL = serverManager.getApiUrl();
    process.env.CURRENT_WORKER_ANGULAR_URL = serverManager.getAngularUrl();
    
    await use(serverManager);
    
    // Cleanup servers for this worker
    await serverManager.stopServers();
    
    console.log(`✅ Worker ${workerInfo.workerIndex} cleanup completed`);
  }, { scope: 'worker' }],

  // Test-scoped fixtures
  workerSetup: async ({ workerServerManager }, use) => {
    await use(workerServerManager);
  },

  workerIndex: async ({ workerSetup }, use) => {
    const workerIndex = parseInt(process.env.CURRENT_WORKER_INDEX || '0', 10);
    await use(workerIndex);
  },

  apiUrl: async ({ workerSetup }, use) => {
    await use(process.env.CURRENT_WORKER_API_URL || 'http://localhost:5172');
  },

  angularUrl: async ({ workerSetup }, use) => {
    await use(process.env.CURRENT_WORKER_ANGULAR_URL || 'http://localhost:4200');
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

  cleanDatabase: [async ({ apiContext, workerIndex }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for worker ${workerIndex}, test: ${testInfo.title}`);
    
    // Pre-test database cleanup and validation
    try {
      const response = await apiContext.post('/api/database/reset', {
        data: { workerIndex }
      });
      
      if (!response.ok()) {
        console.warn(`⚠️ Database reset failed for worker ${workerIndex}: ${response.status()}`);
      } else {
        console.log(`✅ Database reset completed for worker ${workerIndex}`);
      }

      // Validate database state
      const validationResponse = await apiContext.get('/api/database/validate-pre-test', {
        params: { workerIndex: workerIndex.toString() }
      });
      
      if (validationResponse.ok()) {
        const validation = await validationResponse.json();
        if (!validation.isValid) {
          console.warn(`⚠️ Pre-test validation failed for worker ${workerIndex}:`, validation.issues);
        }
      }
    } catch (error) {
      console.error(`❌ Database cleanup failed for worker ${workerIndex}:`, error);
    }
    
    await use();
    
    // Post-test validation (but don't fail the test if validation fails)
    try {
      const validationResponse = await apiContext.get('/api/database/validate-post-test', {
        params: { workerIndex: workerIndex.toString() }
      });
      
      if (validationResponse.ok()) {
        const validation = await validationResponse.json();
        if (!validation.isValid) {
          console.warn(`⚠️ Post-test validation failed for worker ${workerIndex}:`, validation.issues);
        }
      }
    } catch (error) {
      console.error(`❌ Post-test validation failed for worker ${workerIndex}:`, error);
    }
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