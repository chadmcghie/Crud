import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';

/**
 * Serial E2E Test Configuration
 * Based on the decision in docs/02-Architecture/ADR-001-Serial-E2E-Testing.md
 * 
 * Key principles:
 * - Single worker for reliable SQLite/EF Core execution
 * - No parallel execution to avoid database conflicts
 * - Optimized for speed within serial constraints
 * - Test categorization with @smoke, @critical, @extended tags
 */
export default defineConfig({
  testDir: './tests',
  
  /* CRITICAL: Serial execution for SQLite compatibility */
  fullyParallel: false, // Tests run sequentially
  workers: 1, // Single worker to prevent database conflicts
  
  /* Fail fast in CI */
  forbidOnly: !!process.env.CI,
  
  /* No retries - tests should be reliable */
  retries: 0,
  
  /* Circuit breaker: stop after X failures to prevent runaway test execution */
  maxFailures: process.env.CI ? 10 : 0, // Stop after 10 failures in CI
  
  /* Reasonable timeout for serial execution */
  timeout: 30000, // 30 seconds per test
  
  /* Global setup for shared server management */
  globalSetup: './tests/setup/global-setup.ts',
  globalTeardown: './tests/setup/global-teardown.ts',
  
  /* Test categorization using grep patterns */
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
  
  /* Reporter configuration */
  reporter: [
    ['list', { printSteps: true }], // Console output
    ['html', { outputFolder: './test-results/html', open: 'never' }],
    ['json', { outputFile: './test-results/results.json' }],
    ['junit', { outputFile: './test-results/results.xml' }],
    process.env.CI ? ['github'] : null,
  ].filter(Boolean) as any,
  
  /* Output directory for test artifacts */
  outputDir: './test-results/artifacts',
  
  /* Shared settings for all tests */
  use: {
    /* Base URLs - will be set dynamically from shared server */
    baseURL: process.env.ANGULAR_URL || 'http://localhost:4200',
    
    /* Trace collection for debugging */
    trace: process.env.CI ? 'retain-on-failure' : 'off',
    
    /* Screenshots and videos only on failure */
    screenshot: 'only-on-failure',
    video: process.env.CI ? 'retain-on-failure' : 'off',
    
    /* Reasonable timeouts for serial execution */
    actionTimeout: 10000, // 10 seconds for actions
    navigationTimeout: 30000, // 30 seconds for navigation
    
    /* Better error messages */
    expect: {
      timeout: 5000, // 5 seconds for assertions
    },
  },
  
  /* Single browser configuration for speed */
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
    
    /* Cross-browser testing only for critical paths */
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
  ],
  
  /* Environment variables for test execution */
  metadata: {
    testRun: {
      timestamp: new Date().toISOString(),
      mode: 'serial',
      category: process.env.TEST_CATEGORY || 'all',
    },
  },
});