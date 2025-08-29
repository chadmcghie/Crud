import { defineConfig, devices } from '@playwright/test';

/**
 * Single browser configuration to reduce server proliferation
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: false, // Serial execution per ADR-001
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,
  workers: 1, // Single worker per ADR-001 // Reduce workers
  timeout: 60000,
  
  globalSetup: './tests/setup/simple-global-setup.ts',
  globalTeardown: './tests/setup/simple-global-teardown.ts',

  reporter: [
    ['html'],
    ['list', { printSteps: true }]
  ],

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 45000,
  },

  // Only test with Chromium to reduce server instances
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});