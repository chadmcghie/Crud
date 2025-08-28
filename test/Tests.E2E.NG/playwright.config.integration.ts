import { defineConfig, devices } from '@playwright/test';

/**
 * Integration test configuration for Playwright tests
 * Requires both API and Angular servers to be running
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: false, // Integration tests should run sequentially for stability
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Use single worker for integration tests to avoid conflicts */
  workers: 1,
  /* Increase timeout for UI tests */
  timeout: 60000,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html', { outputFolder: './test-results/html' }],
    ['json', { outputFile: './test-results/integration-results.json' }]
  ],
  /* Output directory for test artifacts */
  outputDir: './test-results/',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL for Angular app */
    baseURL: 'http://localhost:4200',
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    /* Record video on failure */
    video: 'retain-on-failure',
    /* Increase timeouts for integration tests */
    actionTimeout: 30000,
    navigationTimeout: 60000,
  },

  /* Configure projects for integration testing */
  projects: [
    {
      name: 'integration-tests',
      use: { ...devices['Desktop Chrome'] },
      testMatch: ['**/integration/*.spec.ts'], // Only integration tests
    },
  ],

  /* 
   * NOTE: Requires both servers to be running:
   * - API server on http://localhost:5172
   * - Angular server on http://localhost:4200
   * Use the LaunchApps.ps1 script to start both servers
   */
});
