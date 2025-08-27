import { defineConfig, devices } from '@playwright/test';

/**
 * CI/CD optimized configuration with pre-built Angular
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  
  /* Run tests in files in parallel */
  fullyParallel: true,
  
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: true,
  
  /* Retry failed tests for stability */
  retries: 2,
  
  /* Use 4 workers on CI for optimal parallelism */
  workers: 4,
  
  /* Timeout configuration */
  timeout: 90000, // 90 seconds per test
  
  /* Global setup and teardown for database management */
  globalSetup: './tests/setup/global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',

  /* Reporter configuration for CI */
  reporter: [
    ['junit', { outputFile: 'test-results/junit.xml' }],
    ['json', { outputFile: 'test-results/results.json' }],
    ['html', { open: 'never', outputFolder: 'test-results/html' }],
    ['github'], // GitHub Actions annotations
    ['list'] // Console output
  ],
  
  /* Shared settings for all the projects below */
  use: {
    /* Base URL to use in actions like `await page.goto('/')` */
    baseURL: 'http://localhost:4200',
    
    /* Collect trace only on failure to save storage */
    trace: 'on-first-retry',
    
    /* Take screenshot on failure */
    screenshot: 'only-on-failure',
    
    /* Record video only on retry to save storage */
    video: 'on-first-retry',
    
    /* Timeouts optimized for CI */
    actionTimeout: 20000,
    navigationTimeout: 30000,
  },

  /* Configure projects for cross-browser testing */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        launchOptions: {
          args: [
            '--disable-dev-shm-usage', // Prevent /dev/shm issues in containers
            '--no-sandbox', // Required for CI environments
            '--disable-setuid-sandbox',
            '--disable-gpu', // Disable GPU in CI
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
            'media.navigator.permission.disabled': true,
          }
        }
      },
    },
    {
      name: 'webkit',
      use: { 
        ...devices['Desktop Safari'],
      },
    },
  ],

  /* Output test results to a specific folder */
  outputDir: 'test-results',

  /* Preserve test outputs */
  preserveOutput: 'failures-only',

  /* Configure test sharding for distributed CI */
  // Can be used with CI matrix builds
  // shard: { current: 1, total: 4 },
});