import { defineConfig, devices } from '@playwright/test';

/**
 * Configuration optimized for system browsers without Playwright downloads
 * This avoids ffmpeg and Playwright browser download issues
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,
  workers: 1,
  timeout: 60000,
  
  reporter: [
    ['html'],
    ['list'],
    ['json', { outputFile: 'test-results.json' }]
  ],

  use: {
    baseURL: 'http://localhost:4200',
    // Disable features that require ffmpeg
    trace: 'off', // Disable tracing to avoid ffmpeg dependency
    screenshot: 'only-on-failure',
    video: 'off', // Disable video to avoid ffmpeg dependency
    actionTimeout: 15000,
    navigationTimeout: 45000,
  },

  projects: [
    // API tests (don't require browser)
    {
      name: 'api-tests',
      testMatch: ['**/api/*.spec.ts'],
      testIgnore: ['**/api/*-parallel.spec.ts'],
      use: {
        baseURL: 'http://localhost:5172',
      },
    },
    
    // UI tests using system Chrome
    {
      name: 'chrome-system',
      testMatch: ['**/angular-ui/*.spec.ts', '**/integration/*.spec.ts'],
      use: { 
        ...devices['Desktop Chrome'],
        channel: 'chrome',
        launchOptions: {
          executablePath: '/usr/bin/google-chrome',
          args: [
            '--no-sandbox',
            '--disable-dev-shm-usage',
            '--disable-extensions',
            '--disable-background-timer-throttling',
            '--disable-backgrounding-occluded-windows',
            '--disable-renderer-backgrounding'
          ]
        }
      },
    },
    
    // UI tests using system Chromium
    {
      name: 'chromium-system',
      testMatch: ['**/angular-ui/*.spec.ts', '**/integration/*.spec.ts'],
      use: { 
        ...devices['Desktop Chrome'],
        channel: 'chromium',
        launchOptions: {
          executablePath: '/usr/bin/chromium-browser',
          args: [
            '--no-sandbox',
            '--disable-dev-shm-usage',
            '--disable-extensions'
          ]
        }
      },
    }
  ],

  /* No webServer - requires manual server startup */
});