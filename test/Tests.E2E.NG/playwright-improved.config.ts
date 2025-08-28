import { defineConfig, devices } from '@playwright/test';

/**
 * Improved Playwright configuration with better server management and Polly patterns
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel with proper worker isolation */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry configuration with exponential backoff consideration */
  retries: process.env.CI ? 2 : 1, // Reduced retries since we have better resilience
  /* Enable parallel workers with improved server management */
  workers: process.env.CI ? 4 : 2, // Use multiple workers with server reuse
  /* Increase timeout for slow startup */
  timeout: 60000, // 60 seconds per test
  
  /* Use improved global setup and teardown */
  globalSetup: './tests/setup/improved-global-setup.ts',
  globalTeardown: './tests/setup/improved-global-teardown.ts',

  /* Reporter configuration */
  reporter: [
    ['html'],
    ['json', { outputFile: './test-results/results.json' }],
    ['junit', { outputFile: './test-results/results.xml' }],
    ['list', { printSteps: false }] // Reduce output spam
  ],
  
  /* Shared settings for all the projects below */
  use: {
    /* Base URL will be overridden per worker */
    baseURL: 'http://localhost:4200',
    /* Collect trace only on failure for performance */
    trace: 'retain-on-failure',
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    /* Record video on failure */
    video: 'retain-on-failure',
    /* Improved timeouts with Polly-like resilience */
    actionTimeout: 20000,
    navigationTimeout: 60000,
    /* Better error messages */
    expect: {
      timeout: 10000
    }
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        /* Reduce browser overhead */
        launchOptions: {
          args: [
            '--disable-dev-shm-usage',
            '--disable-background-timer-throttling',
            '--disable-backgrounding-occluded-windows',
            '--disable-renderer-backgrounding'
          ]
        }
      },
    },

    {
      name: 'firefox',
      use: { 
        ...devices['Desktop Firefox'],
        launchOptions: {
          firefoxUserPrefs: {
            'media.navigator.streams.fake': true,
            'media.navigator.permission.disabled': true
          }
        }
      },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports if needed */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
  ],

  /* Output folder for test artifacts */
  outputDir: './test-results/',

  /* Run your local dev server optimization */
  webServer: undefined, // We manage servers ourselves for better control
  
  /* Environment variables for better control */
  env: {
    // Pre-warm servers for better performance
    PREWARM_SERVERS: process.env.CI ? 'false' : 'true',
    // Cleanup settings
    CLEANUP_BEFORE_TESTS: process.env.CI ? 'true' : 'false',
    CLEANUP_AFTER_TESTS: process.env.CI ? 'true' : 'false',
    // Kill existing servers if ports are in use
    KILL_EXISTING_SERVERS: 'true',
  }
});