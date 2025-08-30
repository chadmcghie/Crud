import { defineConfig, devices } from '@playwright/test';

/**
 * Quiet configuration with minimal output
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* SERIAL EXECUTION - Based on ADR-001 Decision */
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  timeout: 30000, // Increased from 15s to 30s for server startup
  
  /* Global setup and teardown with robust server management */
  globalSetup: './tests/setup/optimized-global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',

  /* Minimal reporter configuration */
  reporter: [
    // Use 'dot' reporter for minimal output (just dots for pass/fail)
    ['dot'],
    // Still save HTML report for debugging failures
    ['html', { outputFolder: './playwright-report', open: 'never' }],
    // JSON for CI/CD integration
    ['json', { outputFile: './test-results/results.json' }],
  ],
  
  /* Output directory for test artifacts */
  outputDir: './test-results/',
  
  use: {
    baseURL: 'http://localhost:4200',
    /* Only capture on failure to reduce output */
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10000,
    navigationTimeout: 15000,
  },

  /* Single browser configuration for speed (ADR-001) */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
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
  ],
  
  /* Quiet mode settings */
  quiet: true, // Suppress verbose output
  
  /* Test categorization using grep patterns (ADR-001) */
  grep: (() => {
    const testCategory = process.env.TEST_CATEGORY || 'all';
    switch (testCategory) {
      case 'smoke': return /@smoke/;
      case 'critical': return /@critical/;
      case 'extended': return /@extended/;
      case 'all':
      default: return undefined;
    }
  })(),
});