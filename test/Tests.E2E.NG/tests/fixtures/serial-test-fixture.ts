import { test as base, expect } from '@playwright/test';
import { resetDatabase, getDatabaseSize } from '../setup/database-utils';
import * as path from 'path';

/**
 * Serial test fixture that handles database cleanup between tests
 */
export const test = base.extend({
  // Automatic database cleanup before each test
  page: async ({ page }, use) => {
    // Reset database via API before test
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    
    // Only log database reset on retry or when not in CI
    if (test.info().retry > 0 || !process.env.CI) {
      console.log(`🔄 Resetting database for test: ${test.info().title}`);
    }
    
    try {
      const response = await page.request.post(`${apiUrl}/api/database/reset`, {
        data: { workerIndex: 0, preserveSchema: true }
      });
      
      if (!response.ok()) {
        console.warn(`Database reset failed: ${response.status()}`);
      }
    } catch (error) {
      // Only log errors on retry or when important
      if (test.info().retry > 0) {
        console.warn(`Could not reset database: ${error}`);
      }
    }
    
    // Set up page with default navigation timeout
    page.setDefaultNavigationTimeout(30000);
    page.setDefaultTimeout(10000);
    
    // Log console errors only on retry
    if (test.info().retry > 0) {
      page.on('console', msg => {
        if (msg.type() === 'error') {
          console.error(`[Browser Error] ${msg.text()}`);
        }
      });
      
      // Log page errors
      page.on('pageerror', error => {
        console.error(`[Page Error] ${error.message}`);
      });
    }
    
    // Use the page
    await use(page);
    
    // Optional: Log database size after test for monitoring
    if (process.env.DATABASE_PATH && process.env.DEBUG_DB) {
      const size = await getDatabaseSize(process.env.DATABASE_PATH);
      console.log(`📊 Database size after test: ${(size / 1024).toFixed(2)} KB`);
    }
  },
  
  // API base URL from environment
  apiUrl: async ({}, use) => {
    const url = process.env.API_URL || 'http://localhost:5172';
    await use(url);
  },
  
  // Angular base URL from environment
  baseURL: async ({}, use) => {
    const url = process.env.ANGULAR_URL || 'http://localhost:4200';
    await use(url);
  },
});

// Export expect for convenience
export { expect };

/**
 * Test tags for categorization
 */
export const tags = {
  smoke: '@smoke',
  critical: '@critical',
  extended: '@extended',
} as const;

/**
 * Helper to add tags to test titles
 */
export function tagTest(title: string, ...testTags: (keyof typeof tags)[]): string {
  const tagString = testTags.map(t => tags[t]).join(' ');
  return `${title} ${tagString}`.trim();
}

/**
 * Custom test helpers
 */
export const helpers = {
  /**
   * Wait for API to be ready
   */
  async waitForApi(page: any, apiUrl: string) {
    const maxAttempts = 30;
    for (let i = 0; i < maxAttempts; i++) {
      try {
        const response = await page.request.get(`${apiUrl}/health`);
        if (response.ok()) {
          return true;
        }
      } catch {
        // Continue trying
      }
      await page.waitForTimeout(1000);
    }
    throw new Error('API did not become ready in time');
  },
  
  /**
   * Create test data via API
   */
  async createTestData(page: any, apiUrl: string, endpoint: string, data: any) {
    const response = await page.request.post(`${apiUrl}/${endpoint}`, {
      data,
      headers: {
        'Content-Type': 'application/json',
      },
    });
    
    if (!response.ok()) {
      const body = await response.text();
      throw new Error(`Failed to create test data: ${response.status()} - ${body}`);
    }
    
    return response.json();
  },
  
  /**
   * Clean up test data via API
   */
  async cleanupTestData(page: any, apiUrl: string, endpoint: string, id: string | number) {
    try {
      await page.request.delete(`${apiUrl}/${endpoint}/${id}`);
    } catch (err) {
      console.warn(`Failed to cleanup test data: ${err}`);
    }
  },
  
  /**
   * Wait for Angular to be ready
   */
  async waitForAngular(page: any) {
    // Wait for Angular to be defined
    await page.waitForFunction(() => {
      return typeof (window as any).ng !== 'undefined';
    }, { timeout: 30000 });
    
    // Wait for Angular to be stable
    await page.evaluate(() => {
      return new Promise((resolve) => {
        const ng = (window as any).ng;
        if (ng && ng.getTestability) {
          const testability = ng.getTestability(document.body);
          if (testability) {
            testability.whenStable(() => resolve(true));
          } else {
            resolve(true);
          }
        } else {
          resolve(true);
        }
      });
    });
  },
};