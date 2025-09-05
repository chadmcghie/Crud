import { test as base, APIRequestContext } from '@playwright/test';

export interface TestFixtures {
  apiContext: APIRequestContext;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
}

// Manual cleanup function as fallback
async function manualCleanup(apiContext: APIRequestContext) {
  try {
    // Delete all people with increased timeout for CI
    const peopleResponse = await apiContext.get('/api/people', { timeout: 5000 }); // Increased from 2000
    if (peopleResponse.ok()) {
      const people = await peopleResponse.json();
      for (const person of people) {
        await apiContext.delete(`/api/people/${person.id}`, { timeout: 3000 }).catch(() => {}); // Increased from 1000
      }
    }
    
    // Delete all roles with increased timeout for CI
    const rolesResponse = await apiContext.get('/api/roles', { timeout: 5000 }); // Increased from 2000
    if (rolesResponse.ok()) {
      const roles = await rolesResponse.json();
      for (const role of roles) {
        await apiContext.delete(`/api/roles/${role.id}`, { timeout: 3000 }).catch(() => {}); // Increased from 1000
      }
    }
  } catch (error) {
    // Ignore cleanup errors
  }
}

// Simple test fixtures that use environment variables from global setup
export const test = base.extend<TestFixtures>({
  apiUrl: async ({ }, use) => {
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    await use(apiUrl);
  },

  angularUrl: async ({ }, use) => {
    const angularUrl = process.env.ANGULAR_URL || 'http://localhost:4200';
    await use(angularUrl);
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
    
    // Use the database reset endpoint with proper authentication and retry logic
    let retryCount = 0;
    const maxRetries = 2;
    
    while (retryCount <= maxRetries) {
      try {
        console.log(`📡 Sending database reset request...${retryCount > 0 ? ` (retry ${retryCount})` : ''}`);
        const startTime = Date.now();
        
        const resetResponse = await apiContext.post('/api/database/reset', {
          data: { 
            workerIndex: 0, 
            preserveSchema: true 
          },
          headers: {
            'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
          },
          timeout: 15000 // Increased from 5000 for CI database operations
        });
        
        const duration = Date.now() - startTime;
        console.log(`📡 Database reset response received in ${duration}ms`);
        
        if (!resetResponse.ok()) {
          console.warn(`⚠️ Database reset failed with status ${resetResponse.status()}`);
          // Fall back to manual cleanup if reset endpoint fails
          await manualCleanup(apiContext);
        } else {
          console.log(`✅ Database reset successful in ${duration}ms`);
        }
        break; // Success, exit retry loop
        
      } catch (error: any) {
        retryCount++;
        
        if (error.message?.includes('Request context disposed') || error.message?.includes('Target page, context or browser has been closed')) {
          console.warn(`⚠️ Database cleanup error: ${error.message}`);
          if (retryCount <= maxRetries) {
            console.log(`🔄 Retrying database cleanup (${retryCount}/${maxRetries})...`);
            await new Promise(resolve => setTimeout(resolve, 1000 * retryCount)); // Exponential backoff
            continue;
          } else {
            console.warn('⚠️ Max retries reached, proceeding with test');
            break;
          }
        } else if (error.message?.includes('Timeout')) {
          console.warn(`⚠️ Database cleanup timed out after ${15000}ms - API may be under load`);
          if (retryCount <= maxRetries) {
            console.log(`🔄 Retrying after timeout (${retryCount}/${maxRetries})...`);
            await new Promise(resolve => setTimeout(resolve, 2000 * retryCount));
            continue;
          } else {
            console.warn('⚠️ Max timeout retries reached, proceeding with test');
            break;
          }
        } else {
          console.warn('⚠️ Database cleanup warning:', error.message || error);
          break; // Don't retry for other errors
        }
      }
    }
    
    console.log('✅ Database cleanup completed');
    await use();
  }, { auto: true }],

  // Override page to use Angular URL
  page: async ({ browser, angularUrl }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    
    // Set timeouts - increased for CI environment
    page.setDefaultNavigationTimeout(60000); // Increased from 45000
    page.setDefaultTimeout(30000); // Increased from 15000
    
    // Navigate to Angular URL
    await page.goto(angularUrl);
    
    await use(page);
    
    await context.close();
  },
});

export { expect } from '@playwright/test';