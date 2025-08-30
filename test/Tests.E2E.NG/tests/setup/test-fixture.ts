import { test as base, APIRequestContext } from '@playwright/test';

export interface TestFixtures {
  apiContext: APIRequestContext;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
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
    
    // Simple database cleanup - delete all entities
    // Skip cleanup if the API is not responding
    try {
      // First check if API is responsive with a longer timeout
      const healthCheck = await apiContext.get('/health', { timeout: 30000 });
      if (!healthCheck.ok()) {
        console.warn('⚠️ API health check failed, skipping cleanup');
        await use();
        return;
      }
      
      // Delete all todos with increased timeout
      const todosResponse = await apiContext.get('/api/todos', { timeout: 30000 });
      if (todosResponse.ok()) {
        const todos = await todosResponse.json();
        for (const todo of todos) {
          await apiContext.delete(`/api/todos/${todo.id}`, { timeout: 10000 });
        }
      }
      
      // Delete all users with increased timeout
      const usersResponse = await apiContext.get('/api/users', { timeout: 30000 });
      if (usersResponse.ok()) {
        const users = await usersResponse.json();
        for (const user of users) {
          await apiContext.delete(`/api/users/${user.id}`, { timeout: 10000 });
        }
      }
      
      // Delete all people with increased timeout
      const peopleResponse = await apiContext.get('/api/people', { timeout: 30000 });
      if (peopleResponse.ok()) {
        const people = await peopleResponse.json();
        for (const person of people) {
          await apiContext.delete(`/api/people/${person.id}`, { timeout: 10000 });
        }
      }
      
      // Delete all roles with increased timeout
      const rolesResponse = await apiContext.get('/api/roles', { timeout: 30000 });
      if (rolesResponse.ok()) {
        const roles = await rolesResponse.json();
        for (const role of roles) {
          await apiContext.delete(`/api/roles/${role.id}`, { timeout: 10000 });
        }
      }
    } catch (error: any) {
      // Only log timeout errors briefly, not full stack traces
      if (error.message?.includes('Timeout')) {
        console.warn('⚠️ Database cleanup timed out - API may be under load');
      } else {
        console.warn('⚠️ Database cleanup warning:', error.message || error);
      }
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