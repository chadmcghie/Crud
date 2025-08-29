import { test as base, APIRequestContext, Page } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';

export interface ImprovedTestFixtures {
  parallelIndex: number;
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
  workerIndex: number;
  serverInfo: { apiUrl: string; angularUrl: string; database: string };
  page: Page;
}

// Improved test fixture that reuses servers efficiently
export const test = base.extend<ImprovedTestFixtures>({
  // Get server info from environment variables
  serverInfo: async ({ }, use, testInfo) => {
    const parallelIndex = testInfo.parallelIndex;
    console.log(`🔧 Getting server info for parallel ${parallelIndex} (worker ${testInfo.workerIndex})...`);
    
    const info = {
      apiUrl: process.env.API_URL || 'http://localhost:5172',
      angularUrl: process.env.ANGULAR_URL || 'http://localhost:4200',
      database: process.env.DATABASE_PATH || ''
    };
    
    await use(info);
  },
  
  parallelIndex: async ({ }, use, testInfo) => {
    await use(testInfo.parallelIndex);
  },
  
  workerIndex: async ({ }, use, testInfo) => {
    await use(testInfo.workerIndex);
  },

  apiUrl: async ({ serverInfo }, use) => {
    await use(serverInfo.apiUrl);
  },

  angularUrl: async ({ serverInfo }, use) => {
    await use(serverInfo.angularUrl);
  },

  apiContext: async ({ playwright, apiUrl }, use) => {
    // Create API context with Polly-like retry configuration
    const context = await playwright.request.newContext({
      baseURL: apiUrl,
      timeout: 30000, // 30 second timeout
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
    
    // Database cleanup with retry logic
    let retries = 3;
    while (retries > 0) {
      try {
        const response = await apiContext.post('/api/database/reset', {
          data: { parallelIndex },
          timeout: 10000
        });
        
        if (response.ok()) {
          console.log(`✅ Database reset completed for parallel ${parallelIndex}`);
          break;
        } else if (response.status() === 404) {
          // Database reset endpoint might not exist, that's OK
          console.log(`ℹ️ Database reset endpoint not found, skipping cleanup`);
          break;
        } else {
          console.warn(`⚠️ Database reset failed with status ${response.status()}, retrying...`);
        }
      } catch (error) {
        console.warn(`⚠️ Database cleanup attempt failed:`, error);
      }
      
      retries--;
      if (retries > 0) {
        await new Promise(resolve => setTimeout(resolve, 1000));
      }
    }
    
    await use();
  }, { auto: true }],

  // Override page to use worker-specific Angular URL with better configuration
  page: async ({ browser, angularUrl }, use) => {
    const context = await browser.newContext({
      // Better viewport for testing
      viewport: { width: 1280, height: 720 },
      // Ignore HTTPS errors for local development
      ignoreHTTPSErrors: true,
    });
    
    const page = await context.newPage();
    
    // Set better timeouts
    page.setDefaultNavigationTimeout(60000); // 60 seconds
    page.setDefaultTimeout(30000); // 30 seconds
    
    // Add request/response logging for debugging
    page.on('requestfailed', request => {
      console.error(`❌ Request failed: ${request.url()} - ${request.failure()?.errorText}`);
    });
    
    // Navigate to worker-specific Angular URL with retry
    let retries = 3;
    while (retries > 0) {
      try {
        await page.goto(angularUrl, {
          waitUntil: 'networkidle',
          timeout: 60000
        });
        break;
      } catch (error) {
        console.warn(`⚠️ Failed to navigate to ${angularUrl}, retry ${4 - retries}/3`);
        retries--;
        if (retries > 0) {
          await new Promise(resolve => setTimeout(resolve, 2000));
        } else {
          throw error;
        }
      }
    }
    
    await use(page);
    
    await context.close();
  },
});

export { expect } from '@playwright/test';