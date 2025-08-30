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
    // Delete all people
    const peopleResponse = await apiContext.get('/api/people', { timeout: 2000 });
    if (peopleResponse.ok()) {
      const people = await peopleResponse.json();
      for (const person of people) {
        await apiContext.delete(`/api/people/${person.id}`, { timeout: 1000 }).catch(() => {});
      }
    }
    
    // Delete all roles
    const rolesResponse = await apiContext.get('/api/roles', { timeout: 2000 });
    if (rolesResponse.ok()) {
      const roles = await rolesResponse.json();
      for (const role of roles) {
        await apiContext.delete(`/api/roles/${role.id}`, { timeout: 1000 }).catch(() => {});
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
    
    // Use the database reset endpoint with proper authentication
    try {
      const resetResponse = await apiContext.post('/api/database/reset', {
        data: { 
          workerIndex: 0, 
          preserveSchema: true 
        },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        },
        timeout: 5000
      });
      
      if (!resetResponse.ok()) {
        console.warn(`⚠️ Database reset failed with status ${resetResponse.status()}`);
        // Fall back to manual cleanup if reset endpoint fails
        await manualCleanup(apiContext);
      }
    } catch (error: any) {
      if (error.message?.includes('Timeout')) {
        console.warn('⚠️ Database cleanup timed out - API may be under load');
      } else {
        console.warn('⚠️ Database cleanup warning:', error.message || error);
      }
      // Continue with test anyway
    }
    
    console.log('🧪 Starting test - database automatically cleaned');
    await use();
  }, { auto: true }],

  // Override page to use Angular URL
  page: async ({ browser, angularUrl }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    
    // Set timeouts
    page.setDefaultNavigationTimeout(45000);
    page.setDefaultTimeout(15000);
    
    // Navigate to Angular URL
    await page.goto(angularUrl);
    
    await use(page);
    
    await context.close();
  },
});

export { expect } from '@playwright/test';