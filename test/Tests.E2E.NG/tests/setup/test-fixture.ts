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
    try {
      // Delete all todos
      const todosResponse = await apiContext.get('/api/todos');
      if (todosResponse.ok()) {
        const todos = await todosResponse.json();
        for (const todo of todos) {
          await apiContext.delete(`/api/todos/${todo.id}`);
        }
      }
      
      // Delete all users
      const usersResponse = await apiContext.get('/api/users');
      if (usersResponse.ok()) {
        const users = await usersResponse.json();
        for (const user of users) {
          await apiContext.delete(`/api/users/${user.id}`);
        }
      }
    } catch (error) {
      console.warn('⚠️ Database cleanup warning:', error);
    }
    
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