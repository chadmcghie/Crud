import { defineConfig, devices } from '@playwright/test';

/**
 * Parallel execution configuration for Playwright tests with true worker isolation
 * Each worker gets its own API server and database for complete isolation
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
  /* Enable parallel execution with multiple workers */
  workers: process.env.CI ? 2 : 3, // Use moderate number of workers for stability
  /* Ensure test isolation */
  globalSetup: undefined,
  globalTeardown: undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results/results.json' }]
  ],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL for shared API server */
    baseURL: 'http://localhost:5172',
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    /* Record video on failure */
    video: 'retain-on-failure',
    /* Increase timeouts for better stability with database resets */
    actionTimeout: 15000,
    navigationTimeout: 30000,
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium-parallel',
      use: { ...devices['Desktop Chrome'] },
      testMatch: ['**/api/*-parallel.spec.ts'], // Only run parallel-specific API tests
    },

    // Uncomment when ready to test with other browsers
    // {
    //   name: 'firefox-parallel',
    //   use: { ...devices['Desktop Firefox'] },
    //   testMatch: ['**/api/*.spec.ts'],
    // },

    // {
    //   name: 'webkit-parallel',
    //   use: { ...devices['Desktop Safari'] },
    //   testMatch: ['**/api/*.spec.ts'],
    // },
  ],

  /* 
   * NOTE: Uses shared API server with database reset for isolation
   * This provides good isolation with better resource efficiency
   * Make sure API server is running on http://localhost:5172
   */
});
