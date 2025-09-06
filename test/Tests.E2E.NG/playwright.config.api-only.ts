import { defineConfig, devices } from '@playwright/test';

/**
 * API-only test configuration for running tests without Angular server
 * Used for testing API endpoints without UI dependencies
 */
export default defineConfig({
  testDir: './tests/api',
  
  /* Serial execution for SQLite compatibility */
  fullyParallel: false,
  workers: 1,
  
  /* Fail fast in CI */
  forbidOnly: !!process.env.CI,
  retries: 0,
  maxFailures: process.env.CI ? 10 : 0,
  
  /* Timeouts */
  timeout: 30000,
  
  /* No global setup - API server must be started manually */
  
  /* Reporter configuration */
  reporter: [
    ['list', { printSteps: true }],
    ['html', { outputFolder: './test-results/html', open: 'never' }],
    ['json', { outputFile: './test-results/results.json' }],
    ['junit', { outputFile: './test-results/results.xml' }],
  ],
  
  /* Output directory */
  outputDir: './test-results/artifacts',
  
  /* Test settings */
  use: {
    baseURL: 'http://localhost:5172',
    
    /* Debugging aids */
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    
    /* Timeouts */
    actionTimeout: 10000,
    navigationTimeout: 30000,
    
    expect: {
      timeout: 5000,
    },
  },
  
  /* Browser configuration - not needed for API tests but required by Playwright */
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
  
  /* Metadata */
  metadata: {
    testRun: {
      timestamp: new Date().toISOString(),
      mode: 'api-only',
      category: process.env.TEST_CATEGORY || 'all',
    },
  },
});
