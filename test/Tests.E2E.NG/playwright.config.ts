import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel with proper worker isolation */
  fullyParallel: true, // Enable full parallelism with worker database isolation
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry configuration */
  retries: process.env.CI ? 3 : 0, // Disable retries locally to avoid server restart overhead
  /* Enable parallel workers with database isolation */
  workers: process.env.CI ? 4 : 2, // Use multiple workers with isolated databases
  /* Increase timeout for slow startup */
  timeout: 60000, // 60 seconds per test
  
  /* Global setup and teardown for database management */
  globalSetup: './tests/setup/global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html', { outputFolder: './test-results/html' }],
    ['json', { outputFile: './test-results/results.json' }],
    ['junit', { outputFile: './test-results/results.xml' }],
    ['list', { printSteps: true }]
  ],
  
  /* Output directory for test artifacts */
  outputDir: './test-results/',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:4200',
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    /* Record video on failure */
    video: 'retain-on-failure',
    /* Increase timeouts for better stability */
    actionTimeout: 15000,
    navigationTimeout: 45000,
    /* Wait for network to be idle before considering navigation complete */
    // waitForLoadState: 'networkidle', // Removed - not a valid option in use block
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Per-worker servers are started dynamically in test setup - removed static webServer configuration */
});