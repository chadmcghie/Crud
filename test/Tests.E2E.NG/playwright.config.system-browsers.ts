import { defineConfig, devices } from '@playwright/test';

/**
 * Configuration using system-installed browsers
 * Fallback when Playwright browser downloads fail
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  timeout: 60000,
  reporter: 'html',
  
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 45000,
  },

  projects: [
    {
      name: 'chromium-system',
      use: { 
        ...devices['Desktop Chrome'],
        channel: 'chrome', // Use system Chrome
      },
    },
    {
      name: 'firefox-system',
      use: { 
        ...devices['Desktop Firefox'],
        channel: 'firefox', // Use system Firefox
      },
    },
    // Skip webkit for now due to system dependency issues
  ],

  /* No webServer - use manually started servers */
});