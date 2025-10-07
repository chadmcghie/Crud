import { test as base, expect } from '@playwright/test';
import { resetDatabase, getDatabaseSize } from '../setup/database-utils';
import { setupTestAuthentication, clearTestAuthentication } from '../helpers/test-auth-setup';
import * as path from 'path';

/**
 * Serial test fixture that handles database cleanup between tests
 * Environment-aware configuration for CI/local differences
 */

// Environment detection
const isCI = !!process.env.CI;
const isWindows = process.platform === 'win32';
const debugMode = process.env.DEBUG_E2E === 'true';

export const test = base.extend<{ apiUrl: string; baseURL: string }>({
  // Environment-aware page setup with automatic database cleanup
  page: async ({ page }, use) => {
    // Setup test authentication using the dedicated helper
    await setupTestAuthentication(page, {
      userId: 'e2e-test-user',
      email: 'e2e@test.com',
      roles: ['User', 'Admin'],
      useLocalStorage: true
    });

    // Reset database via API before test with environment-specific handling
    const apiUrl = process.env.API_URL || 'http://localhost:5172';

    // Environment-specific logging strategy
    const shouldLog = test.info().retry > 0 || !isCI || debugMode;
    if (shouldLog) {
      console.log(`🔄 Resetting database for test: ${test.info().title}`);
    }

    try {
      const response = await page.request.post(`${apiUrl}/api/database/reset`, {
        data: { workerIndex: 0, preserveSchema: true },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        },
        // Environment-specific timeout
        timeout: isCI ? 30000 : 15000
      });

      if (!response.ok()) {
        const errorMsg = `Database reset failed: ${response.status()}`;
        if (shouldLog) console.warn(errorMsg);
      }
    } catch (error) {
      // Environment-aware error logging
      if (test.info().retry > 0 || debugMode) {
        console.warn(`Could not reset database: ${error}`);
      }
    }

    // Environment-specific page timeout configuration
    const navTimeout = isCI ? 45000 : 30000;
    const defaultTimeout = isCI ? 30000 : 20000;

    page.setDefaultNavigationTimeout(navTimeout);
    page.setDefaultTimeout(defaultTimeout);

    // Environment-aware error logging
    if (test.info().retry > 0 || debugMode) {
      page.on('console', msg => {
        if (msg.type() === 'error') {
          console.error(`[Browser Error] ${msg.text()}`);
        }
      });

      // Log page errors with environment context
      page.on('pageerror', error => {
        console.error(`[Page Error] ${error.message}`);
      });

      // Additional CI-specific error handling
      if (isCI) {
        page.on('requestfailed', request => {
          console.error(`[Request Failed] ${request.url()} - ${request.failure()?.errorText}`);
        });
      }
    }

    // Use the page
    await use(page);

    // Clean up test authentication
    await clearTestAuthentication(page);

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