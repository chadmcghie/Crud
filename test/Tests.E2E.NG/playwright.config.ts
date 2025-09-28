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

// Environment validation and setup
const isCI = !!process.env.CI;
const isWindows = process.platform === 'win32';
const testCategory = process.env.TEST_CATEGORY || 'all';

// Generate unique database name for this test run with better collision avoidance
const testRunId = isCI
  ? `ci-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
  : `local-${Date.now()}`;
const databasePath = path.join(process.cwd(), '..', '..', `CrudTest_${testRunId}.db`);

// Export test run ID for teardown
process.env.TEST_RUN_ID = testRunId;
process.env.DATABASE_PATH = databasePath;

export default defineConfig({
  testDir: './tests',

  /* Serial execution for SQLite compatibility */
  fullyParallel: false,
  workers: 1,

  /* Fail fast in CI with improved retry strategy */
  forbidOnly: isCI,
  retries: isCI ? 1 : 0, // Allow one retry in CI for flaky network issues
  maxFailures: isCI ? 10 : 0,

  /* Timeouts - environment-optimized */
  timeout: isCI ? 90000 : 45000, // Increased for slower CI environments

  /* Playwright's built-in webServer configuration */
  webServer: [
    {
      // API Server configuration with environment-specific optimizations
      command: isCI
        ? `cd ${path.join(process.cwd(), '..', '..')} && dotnet run --project src/Api/Api.csproj --launch-profile testing`
        : 'dotnet run --project ../../src/Api/Api.csproj --launch-profile testing',
      cwd: isCI ? undefined : process.cwd(),
      url: 'http://localhost:5172/health',
      timeout: isCI ? 120 * 1000 : 90 * 1000, // More time for CI environment
      reuseExistingServer: !isCI, // Reuse locally, fresh in CI
      stdout: isCI ? 'pipe' : 'ignore', // Show output in CI for debugging
      stderr: isCI ? 'pipe' : 'ignore', // Show errors in CI for debugging
      env: {
        // EXPLICITLY Testing configuration only - never multi-config
        ASPNETCORE_ENVIRONMENT: 'Testing',
        ASPNETCORE_URLS: isCI
          ? 'http://0.0.0.0:5172'  // Bind to all interfaces in CI
          : 'http://localhost:5172',

        // Testing-optimized database configuration with better isolation
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: isCI
          ? `Data Source=${databasePath};Cache=Private;Pooling=False;Mode=ReadWriteCreate;Journal Mode=WAL;`
          : `Data Source=${databasePath};Journal Mode=WAL;`,

        // Environment-specific database settings
        DATABASE_TIMEOUT: isCI ? '30' : '10',
        SQLITE_BUSY_TIMEOUT: '30000',

        // Testing-specific features
        TEST_RESET_TOKEN: 'test-only-token',
        BYPASS_AUTHORIZATION_FOR_E2E: 'true',
        E2E_TEST_MODE: 'true',
        BYPASS_AUTHORIZATION_FOR_INTEGRATION: 'true', // Additional bypass for E2E

        // Testing environment logging (minimal for performance)
        Logging__LogLevel__Default: 'Warning',
        Logging__LogLevel__Microsoft: 'Warning',
        Logging__LogLevel__System: 'Warning',

        // Testing-specific feature flags
        OutputCaching__Disabled: 'false',
        Caching__UseRedis: 'false',

        // Performance optimizations for Testing
        DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: '1',
        DOTNET_RUNNING_IN_CONTAINER: 'false'
      },
    },
    {
      // Angular Server configuration with environment optimizations
      command: isCI
        ? `cd ${path.join(process.cwd(), '..', '..', 'src', 'Angular')} && npm run start:ci`
        : 'npm start',
      cwd: isCI ? undefined : path.join(process.cwd(), '..', '..', 'src', 'Angular'),
      url: 'http://localhost:4200',
      timeout: isCI ? 180 * 1000 : 120 * 1000, // More time for CI environment compilation
      reuseExistingServer: !isCI,
      stdout: isCI ? 'pipe' : 'ignore', // Show output in CI for debugging
      stderr: isCI ? 'pipe' : 'ignore', // Show errors in CI for debugging
      env: {
        PORT: '4200',
        API_URL: 'http://localhost:5172',
        PATH: process.env.PATH, // Ensure PATH is inherited for CI
        // Angular environment-specific optimizations
        NODE_OPTIONS: isCI ? '--max-old-space-size=4096' : '--max-old-space-size=2048',
        FORCE_COLOR: '1', // Ensure colored output in CI logs
      },
    }
  ],

  /* Test categorization with better environment handling */
  grep: (() => {
    switch (testCategory) {
      case 'smoke': return /@smoke/;
      case 'critical': return /@critical/;
      case 'extended': return /@extended/;
      case 'all':
      default: return undefined;
    }
  })(),

  /* Reporter configuration - environment-optimized output */
  reporter: isCI
    ? [
        ['dot'],  // Minimal console output for CI
        ['junit', { outputFile: './test-results/results.xml' }],  // For CI test publishing
        ['github'],  // GitHub annotations
        ['json', { outputFile: './test-results/results.json' }]  // Machine-readable results
      ]
    : [
        ['list', { printSteps: false }],  // Local development
        ['html', { outputFolder: './test-results/html', open: 'never' }],
        ['json', { outputFile: './test-results/results.json' }]  // Local analysis
      ],

  /* Output directory */
  outputDir: './test-results/artifacts',

  /* Test settings */
  use: {
    baseURL: 'http://localhost:4200',

    /* API base URL for backend tests */
    extraHTTPHeaders: {
      'X-Test-Run-Id': testRunId.toString(),
      'X-E2E-Test': 'true', // Additional E2E marker
    },

    /* Debugging aids - environment-optimized */
    trace: isCI ? 'retain-on-failure' : 'off',
    screenshot: 'only-on-failure',
    video: isCI ? 'retain-on-failure' : 'off',

    /* Timeouts - environment-specific tuning */
    actionTimeout: isCI ? 30000 : 20000, // More time for CI environments
    navigationTimeout: isCI ? 45000 : 30000, // Slower CI networks

    /* Network-specific configurations */
    ...(isCI && {
      // Additional CI-specific settings for network reliability
      bypassCSP: true, // Bypass CSP in CI for better reliability
      ignoreHTTPSErrors: true, // Ignore HTTPS errors in CI environments
    }),
  },

  /* Expect configuration - environment-tuned */
  expect: {
    timeout: isCI ? 30000 : 20000, // More generous timeouts for CI
    // Softer assertions in CI to handle timing variations
    ...(isCI && {
      poll: 1000, // Check every second in CI
      interval: 500, // Check interval for polling assertions
    }),
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
            // Fix localStorage access in CI environments
            '--disable-features=VizDisplayCompositor',
            '--allow-running-insecure-content',
            '--disable-background-timer-throttling',
            '--disable-backgrounding-occluded-windows',
            '--disable-renderer-backgrounding',
            // Windows-specific optimizations
            ...(isWindows ? [
              '--disable-features=VizDisplayCompositor',
              '--disable-software-rasterizer',
            ] : []),
            // Additional CI environment flags for stability
            ...(isCI ? [
              '--disable-extensions',
              '--disable-default-apps',
              '--disable-component-extensions-with-background-pages',
              '--disable-ipc-flooding-protection',
              '--memory-pressure-off', // Disable memory pressure detection
              '--max_old_space_size=4096', // Increase memory limit
              '--disable-backgrounding-occluded-windows',
              '--disable-features=TranslateUI,BlinkGenPropertyTrees',
              // Enhanced localStorage access for CI environments
              '--disable-features=VizDisplayCompositor,PrivacySandboxSettings4',
              '--disable-site-isolation-trials',
              '--disable-features=VizService',
              '--disable-blink-features=BlockCredentialedSubresources',
              '--allow-file-access-from-files',
              '--disable-web-security-restrictions',
            ] : []),
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
  // globalTeardown: './tests/setup/webserver-teardown.ts',

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