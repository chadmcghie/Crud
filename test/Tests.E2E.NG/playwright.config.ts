import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* SERIAL EXECUTION - Based on ADR-001 Decision */
  fullyParallel: false, // Disable parallel execution for SQLite compatibility
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* No retries - tests should be reliable */
  retries: 0, // No retries as per ADR-001 for reliable tests
  /* Single worker for serial execution */
  workers: 1, // Single worker to prevent database conflicts (ADR-001)
  /* Circuit breaker: stop after X failures to prevent runaway test execution */
  maxFailures: process.env.CI ? 10 : 0, // Stop after 10 failures in CI
  /* Reasonable timeout for serial execution */
  timeout: 15000, // 15 seconds per test should be plenty without arbitrary waits
  
  /* Global setup and teardown for optimized server management */
  globalSetup: './tests/setup/optimized-global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html', { outputFolder: './playwright-report' }],
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
    /* Reasonable timeouts without arbitrary waits */
    actionTimeout: 10000,
    navigationTimeout: 15000,
    /* Wait for network to be idle before considering navigation complete */
    // waitForLoadState: 'networkidle', // Removed - not a valid option in use block
  },

  /* Single browser configuration for speed (ADR-001) */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        /* Optimize for speed */
        launchOptions: {
          args: [
            '--disable-blink-features=AutomationControlled',
            '--disable-dev-shm-usage',
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--disable-gpu',
            '--disable-web-security',
            '--disable-features=IsolateOrigins,site-per-process',
          ],
        },
      },
    },
    
    /* Cross-browser testing only when explicitly requested */
    ...(process.env.CROSS_BROWSER === 'true' ? [
      {
        name: 'firefox',
        use: { ...devices['Desktop Firefox'] },
      },
      {
        name: 'webkit',
        use: { ...devices['Desktop Safari'] },
      },
    ] : []),

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
  
  /* Test categorization using grep patterns (ADR-001) */
  grep: (() => {
    const testCategory = process.env.TEST_CATEGORY || 'all';
    switch (testCategory) {
      case 'smoke': return /@smoke/; // 2 minute tests
      case 'critical': return /@critical/; // 5 minute tests  
      case 'extended': return /@extended/; // 10 minute tests
      case 'all':
      default: return undefined; // Run all tests
    }
  })(),
});