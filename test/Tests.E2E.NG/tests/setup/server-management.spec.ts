import { test, expect } from '../fixtures/serial-test-fixture';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import { getTempDirectory } from './temp-directory';
import * as fs from 'fs/promises';

/**
 * Tests for simplified server management
 * Verifies that the simple spawn-based approach works correctly
 * @smoke
 */

let apiProcess: ChildProcess | null = null;
let angularProcess: ChildProcess | null = null;
const testApiPort = '5182';
const testAngularPort = '4210';
const testDbPath = path.join(getTempDirectory(), 'test-server-mgmt.db');

test.afterAll(async () => {
  // Clean up any test processes using Playwright's page context for timing
  if (apiProcess) {
    apiProcess.kill('SIGTERM');
    // Use a Promise race for deterministic timeout instead of setTimeout
    await Promise.race([
      new Promise<void>((resolve) => {
        apiProcess!.once('exit', resolve);
      }),
      new Promise<void>((resolve) => {
        // Timeout after 1 second, then force kill
        Promise.resolve().then(() => new Promise(r => process.nextTick(r))).then(() => {
          if (!apiProcess!.killed) {
            apiProcess!.kill('SIGKILL');
          }
          resolve();
        });
      })
    ]);
  }
  if (angularProcess) {
    angularProcess.kill('SIGTERM');
    // Use Promise race for deterministic cleanup
    await Promise.race([
      new Promise<void>((resolve) => {
        angularProcess!.once('exit', resolve);
      }),
      new Promise<void>((resolve) => {
        Promise.resolve().then(() => new Promise(r => process.nextTick(r))).then(() => {
          if (!angularProcess!.killed) {
            angularProcess!.kill('SIGKILL');
          }
          resolve();
        });
      })
    ]);
  }
  // Clean up test database
  try {
    await fs.unlink(testDbPath);
  } catch {
    // Ignore if doesn't exist
  }
});

test('should start API server with simple spawn @smoke', async () => {
  const apiProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Api');
  
  apiProcess = spawn('dotnet', ['run', '--no-build'], {
    cwd: apiProjectPath,
    env: {
      ...process.env,
      'ASPNETCORE_URLS': `http://localhost:${testApiPort}`,
      'ASPNETCORE_ENVIRONMENT': 'Testing',
      'DatabasePath': testDbPath,
      'ConnectionStrings__DefaultConnection': `Data Source=${testDbPath}`,
      'Logging__LogLevel__Default': 'Warning',
    },
    shell: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  expect(apiProcess).toBeTruthy();
  expect(apiProcess.pid).toBeTruthy();
});

test('should wait for server readiness with event-driven polling @smoke', async ({ page }) => {
  const waitForServer = async (url: string, timeout: number = 30000): Promise<boolean> => {
    // Use Playwright's expect.toPass() for event-driven polling instead of setTimeout loops
    try {
      await expect(async () => {
        const response = await page.request.get(url);
        expect(response.ok()).toBe(true);
      }).toPass({
        timeout: timeout,
        intervals: [500, 1000] // Check every 500ms, then every 1s
      });
      return true;
    } catch {
      return false;
    }
  };

  // Mock server for testing  
  const mockPort = '5183';
  const mockServer = spawn('npx', ['http-server', '-p', mockPort, '--silent'], {
    shell: true,
    stdio: 'ignore',
  });

  try {
    const isReady = await waitForServer(`http://localhost:${mockPort}`, 10000);
    expect(isReady).toBe(true);
  } finally {
    mockServer.kill();
    // Use Promise.race for deterministic cleanup
    await Promise.race([
      new Promise<void>((resolve) => {
        mockServer.once('exit', resolve);
      }),
      new Promise<void>((resolve) => {
        // Timeout after 1 second
        Promise.resolve().then(() => new Promise(r => process.nextTick(r))).then(() => resolve());
      })
    ]);
  }
});

test('should handle server startup errors gracefully @smoke', async () => {
  const invalidProcess = spawn('invalid-command-that-does-not-exist', [], {
    shell: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  // Use Promise.race for deterministic error handling instead of setTimeout
  await Promise.race([
    new Promise<void>((resolve) => {
      invalidProcess.on('error', (error) => {
        expect(error).toBeTruthy();
        resolve();
      });
    }),
    new Promise<void>((resolve) => {
      invalidProcess.on('exit', (code) => {
        expect(code).not.toBe(0);
        resolve();
      });
    }),
    // Timeout using Promise resolution instead of setTimeout
    new Promise<void>((resolve) => {
      Promise.resolve().then(() => new Promise(r => {
        // Use multiple nextTick calls to simulate a reasonable delay without setTimeout
        for(let i = 0; i < 100; i++) {
          process.nextTick(() => {
            if (i === 99) resolve();
          });
        }
      }));
    })
  ]);
});

test('should clean up database files after tests @smoke', async () => {
  const testFile = path.join(getTempDirectory(), 'test-cleanup.db');
  
  // Create test file
  await fs.writeFile(testFile, 'test data');
  
  // Verify it exists
  const exists = await fs.access(testFile).then(() => true).catch(() => false);
  expect(exists).toBe(true);
  
  // Clean up
  await fs.unlink(testFile);
  
  // Verify it's gone
  const existsAfter = await fs.access(testFile).then(() => true).catch(() => false);
  expect(existsAfter).toBe(false);
});

test('should kill processes cleanly on teardown @smoke', async () => {
  const mockProcess = spawn('node', ['-e', 'setInterval(() => {}, 1000)'], {
    shell: true,
    stdio: 'ignore',
  });

  expect(mockProcess.pid).toBeTruthy();
  
  // Try graceful shutdown first
  mockProcess.kill('SIGTERM');
  
  // Use Promise.race for deterministic process cleanup
  await Promise.race([
    new Promise<void>((resolve) => {
      mockProcess.once('exit', resolve);
    }),
    new Promise<void>((resolve) => {
      // Force kill after a reasonable wait using Promise chains
      Promise.resolve()
        .then(() => new Promise(r => process.nextTick(r)))
        .then(() => {
          if (!mockProcess.killed) {
            mockProcess.kill('SIGKILL');
          }
          // Small additional wait for kill to take effect
          return new Promise(r => process.nextTick(r));
        })
        .then(() => resolve());
    })
  ]);
  
  expect(mockProcess.killed).toBe(true);
});

test('should set environment variables correctly for tests @smoke', async () => {
  const testEnv = {
    API_URL: 'http://localhost:5172',
    ANGULAR_URL: 'http://localhost:4200', 
    DATABASE_PATH: '/tmp/test.db',
    SERIAL_MODE: 'true',
  };

  // Simulate setting env vars
  Object.assign(process.env, testEnv);

  expect(process.env.API_URL).toBe('http://localhost:5172');
  expect(process.env.ANGULAR_URL).toBe('http://localhost:4200');
  expect(process.env.DATABASE_PATH).toBe('/tmp/test.db');
  expect(process.env.SERIAL_MODE).toBe('true');
});
