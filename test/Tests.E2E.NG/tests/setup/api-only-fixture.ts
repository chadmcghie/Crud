import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';

export interface ApiOnlyFixtures {
  apiContext: APIRequestContext;
  apiHelpers: ApiHelpers;
  apiUrl: string;
  cleanDatabase: void;
}

// API-only test fixtures using environment variables from global setup
export const test = base.extend<ApiOnlyFixtures>({
  apiUrl: async ({ }, use) => {
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    await use(apiUrl);
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

  apiHelpers: async ({ apiContext }, use) => {
    const helpers = new ApiHelpers(apiContext, 0); // Single worker, always index 0
    await use(helpers);
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
      
      // Delete all people
      const peopleResponse = await apiContext.get('/api/people');
      if (peopleResponse.ok()) {
        const people = await peopleResponse.json();
        for (const person of people) {
          await apiContext.delete(`/api/people/${person.id}`);
        }
      }
      
      // Delete all roles
      const rolesResponse = await apiContext.get('/api/roles');
      if (rolesResponse.ok()) {
        const roles = await rolesResponse.json();
        for (const role of roles) {
          await apiContext.delete(`/api/roles/${role.id}`);
        }
      }
      
      // Delete all walls
      const wallsResponse = await apiContext.get('/api/walls');
      if (wallsResponse.ok()) {
        const walls = await wallsResponse.json();
        for (const wall of walls) {
          await apiContext.delete(`/api/walls/${wall.id}`);
        }
      }
    } catch (error) {
      console.warn('⚠️ Database cleanup warning:', error);
    }
    
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';