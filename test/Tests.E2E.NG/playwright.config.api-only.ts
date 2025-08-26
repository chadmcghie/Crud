import { defineConfig, devices } from '@playwright/test';

/**
 * API-only configuration for Playwright tests
 * This version only runs API tests and doesn't require Angular or browser UI
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  /* Use only 1 worker to avoid race conditions with in-memory data store */
  workers: 1,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL for API requests */
    baseURL: 'http://localhost:5172',
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
  },

  /* Configure projects for API testing */
  projects: [
    {
      name: 'api-tests',
      use: { 
        // API tests don't need browser context
      },
      testMatch: ['**/api/*.spec.ts'], // Exclude parallel tests
      testIgnore: ['**/api/*-parallel.spec.ts'], // Explicitly ignore parallel tests
    },
  ],

  /* 
   * NOTE: No webServer configuration - you need to manually start the API server on http://localhost:5172
   */
});