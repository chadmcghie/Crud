import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';

/**
 * E2E Test Configuration with Playwright's Built-in WebServer
 * 
 * This configuration uses Playwright's native webServer feature to manage
 * server lifecycle, eliminating the need for custom server management code.
 * 
 * Key improvements:
 * - Automatic server startup and shutdown
 * - Unique database file per test run to prevent locking
 * - Simpler configuration and maintenance
 * - Better CI/CD reliability
 */

// Generate unique database name for this test run
const testRunId = process.env.CI ? Date.now().toString() : 'local';
const databasePath = path.join(process.cwd(), '..', '..', `CrudTest_${testRunId}.db`);

// Export test run ID for teardown
process.env.TEST_RUN_ID = testRunId;

export default defineConfig({
  testDir: './tests',
  
  /* Serial execution for SQLite compatibility */
  fullyParallel: false,
  workers: 1,
  
  /* Fail fast in CI */
  forbidOnly: !!process.env.CI,
  retries: 0,
  maxFailures: process.env.CI ? 10 : 0,
  
  /* Timeouts */
  timeout: 30000,
  
  /* Playwright's built-in webServer configuration */
  webServer: [
    {
      // API Server configuration
      command: 'dotnet run --project ../../src/Api/Api.csproj --launch-profile http',
      cwd: process.cwd(),
      url: 'http://localhost:5172/health',
      timeout: 60 * 1000, // 60 seconds to start
      reuseExistingServer: !process.env.CI, // Reuse locally, fresh in CI
      stdout: 'ignore',
      stderr: 'ignore',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Testing',
        ASPNETCORE_URLS: process.env.CI 
          ? 'http://0.0.0.0:5172'  // Bind to all interfaces in CI
          : 'http://localhost:5172',
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
        TEST_RESET_TOKEN: 'test-only-token',
        // Disable connection pooling in CI to avoid locking
        ...(process.env.CI && {
          'ConnectionStrings__DefaultConnection': `Data Source=${databasePath};Cache=Private;Pooling=False;Mode=ReadWriteCreate;`
        })
      },
    },
    {
      // Angular Server configuration
      command: process.env.CI ? 'npm run start:ci' : 'npm start',
      cwd: path.join(process.cwd(), '..', '..', 'src', 'Angular'),
      url: 'http://localhost:4200',
      timeout: 120 * 1000, // 2 minutes for Angular compilation
      reuseExistingServer: !process.env.CI,
      stdout: 'ignore',
      stderr: 'ignore',
      env: {
        PORT: '4200',
        API_URL: 'http://localhost:5172',
      },
    }
  ],
  
  /* Test categorization */
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
  
  /* Reporter configuration */
  reporter: [
    ['list', { printSteps: true }],
    ['html', { outputFolder: './test-results/html', open: 'never' }],
    ['json', { outputFile: './test-results/results.json' }],
    ['junit', { outputFile: './test-results/results.xml' }],
    process.env.CI ? ['github'] : null,
  ].filter(Boolean) as any,
  
  /* Output directory */
  outputDir: './test-results/artifacts',
  
  /* Test settings */
  use: {
    baseURL: 'http://localhost:4200',
    
    /* API base URL for backend tests */
    extraHTTPHeaders: {
      'X-Test-Run-Id': testRunId.toString(),
    },
    
    /* Debugging aids */
    trace: process.env.CI ? 'retain-on-failure' : 'off',
    screenshot: 'only-on-failure',
    video: process.env.CI ? 'retain-on-failure' : 'off',
    
    /* Timeouts */
    actionTimeout: 10000,
    navigationTimeout: 30000,
    
    expect: {
      timeout: 5000,
    },
  },
  
  /* Browser configuration */
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
    
    /* Cross-browser testing */
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
  
  /* Global teardown for cleanup */
  globalTeardown: './tests/setup/webserver-teardown.ts',
  
  /* Metadata */
  metadata: {
    testRun: {
      timestamp: new Date().toISOString(),
      mode: 'webserver',
      databaseFile: path.basename(databasePath),
      category: process.env.TEST_CATEGORY || 'all',
    },
  },
});