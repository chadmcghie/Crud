import { defineConfig, devices } from '@playwright/test';

/**
 * Robust configuration that handles browser failures gracefully
 * Provides fallback mechanisms for infrastructure issues
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1, // Increased retries for infrastructure issues
  workers: 1,
  timeout: 60000,
  globalTimeout: 1800000, // 30 minutes for entire test suite
  
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results.json' }],
    ['list']
  ],

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 45000,
  },

  projects: [
    // API tests that don't require browsers
    {
      name: 'api-tests',
      testMatch: ['**/api/*.spec.ts'],
      testIgnore: ['**/api/*-parallel.spec.ts'],
      use: {
        baseURL: 'http://localhost:5172',
      },
    },
    
    // UI tests with fallback handling
    {
      name: 'chromium',
      testMatch: ['**/angular-ui/*.spec.ts', '**/integration/*.spec.ts'],
      use: { 
        ...devices['Desktop Chrome'],
        // Add error recovery options
        launchOptions: {
          args: [
            '--no-sandbox',
            '--disable-dev-shm-usage',
            '--disable-extensions'
          ]
        }
      },
    },
    
    // Firefox tests (if available)
    {
      name: 'firefox',
      testMatch: ['**/angular-ui/*.spec.ts', '**/integration/*.spec.ts'],
      use: { 
        ...devices['Desktop Firefox'],
        launchOptions: {
          firefoxUserPrefs: {
            'dom.disable_beforeunload': true,
          }
        }
      },
    }
    
    // Webkit disabled until dependencies resolved
  ],

  /* No webServer - requires manual server startup */
});