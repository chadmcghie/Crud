import { defineConfig, devices } from '@playwright/test';

/**
 * Optimized configuration for faster test startup
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel with proper worker isolation */
  fullyParallel: false, // Serial execution per ADR-001
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Reduced retries for faster feedback */
  retries: process.env.CI ? 1 : 0, // Reduced from 2 to 1 for faster failure
  /* Use fewer workers to reduce startup overhead */
  workers: 1, // Single worker per ADR-001
  /* Circuit breaker: stop after X failures to prevent runaway test execution */
  maxFailures: process.env.CI ? 5 : 0, // Stop after 5 failures in CI, no limit locally
  /* Increase timeout for slow startup */
  timeout: 30000, // Reduced to 30 seconds per test for faster feedback
  
  /* Global setup and teardown for database management */
  globalSetup: './tests/setup/optimized-global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['list', { printSteps: true }],
    ['html', { open: 'never' }]
  ],
  
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:4200',
    /* Collect trace only on failure for performance */
    trace: 'on-first-retry',
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    /* Skip video recording for performance */
    video: 'off',
    /* Increase timeouts for better stability */
    actionTimeout: 30000,
    navigationTimeout: 60000,
  },

  /* Configure projects - use single browser for faster execution */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        /* Use persistent context for faster startup */
        launchOptions: {
          args: [
            '--disable-dev-shm-usage',
            '--disable-web-security',
            '--disable-features=IsolateOrigins,site-per-process',
            '--no-sandbox'
          ]
        }
      },
    },
  ],
});