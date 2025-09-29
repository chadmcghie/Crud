import { test as base, expect } from '@playwright/test';
import { resetDatabase } from '../setup/simple-database-utils';

/**
 * Simple test fixture with basic database cleanup
 * No complex monitoring or state management
 */
export const test = base.extend({
  // Simple database cleanup before each test
  page: async ({ page }, use) => {
    // Reset database if path is set
    if (process.env.DATABASE_PATH) {
      await resetDatabase(process.env.DATABASE_PATH);
    }
    
    // Basic page setup
    page.setDefaultNavigationTimeout(30000);
    page.setDefaultTimeout(10000);
    
    // Use the page
    await use(page);
  },
  
  // API URL from environment
  apiUrl: async ({}, use) => {
    await use(process.env.API_URL || 'http://localhost:5172');
  },
  
  // Angular URL from environment
  baseURL: async ({}, use) => {
    await use(process.env.ANGULAR_URL || 'http://localhost:4200');
  },
});

// Export expect for convenience
export { expect };

/**
 * Simple test helpers
 */
export const helpers = {
  /**
   * Wait for API health endpoint
   */
  async waitForApi(page: any, apiUrl: string): Promise<void> {
    const maxAttempts = 30;
    for (let i = 0; i < maxAttempts; i++) {
      try {
        const response = await page.request.get(`${apiUrl}/health`);
        if (response.ok()) return;
      } catch {
        // Keep trying
      }
      await page.waitForTimeout(1000);
    }
    throw new Error('API not ready');
  },
  
  /**
   * Create test data via API
   */
  async createTestData(page: any, apiUrl: string, endpoint: string, data: any) {
    const response = await page.request.post(`${apiUrl}/${endpoint}`, {
      data,
      headers: { 'Content-Type': 'application/json' },
    });
    
    if (!response.ok()) {
      throw new Error(`Failed to create test data: ${response.status()}`);
    }
    
    return response.json();
  },
  
  /**
   * Delete test data via API
   */
  async deleteTestData(page: any, apiUrl: string, endpoint: string, id: string | number) {
    await page.request.delete(`${apiUrl}/${endpoint}/${id}`).catch(() => {
      // Ignore cleanup errors
    });
  },
};