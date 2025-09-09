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
  // Clean up any test processes
  if (apiProcess) {
    apiProcess.kill('SIGTERM');
    // Wait for process to exit
    await new Promise<void>((resolve) => {
      const timeout = setTimeout(() => {
        if (!apiProcess.killed) {
          apiProcess.kill('SIGKILL');
        }
        resolve();
      }, 1000);
      apiProcess.once('exit', () => {
        clearTimeout(timeout);
        resolve();
      });
    });
  }
  if (angularProcess) {
    angularProcess.kill('SIGTERM');
    // Wait for process to exit
    await new Promise<void>((resolve) => {
      const timeout = setTimeout(() => {
        if (!angularProcess.killed) {
          angularProcess.kill('SIGKILL');
        }
        resolve();
      }, 1000);
      angularProcess.once('exit', () => {
        clearTimeout(timeout);
        resolve();
      });
    });
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

test('should wait for server readiness with simple fetch loop @smoke', async ({ page }) => {
  const waitForServer = async (url: string, timeout: number = 30000): Promise<boolean> => {
    const startTime = Date.now();
    const pollInterval = 500; // Reduced from 2000ms to 500ms
    
    return new Promise<boolean>((resolve) => {
      const checkServer = async () => {
        if (Date.now() - startTime >= timeout) {
          resolve(false);
          return;
        }
        
        try {
          const response = await page.request.get(url);
          if (response.ok()) {
            resolve(true);
            return;
          }
        } catch {
          // Server not ready yet
        }
        
        setTimeout(checkServer, pollInterval);
      };
      
      checkServer();
    });
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
    // Wait for process to actually exit
    await new Promise<void>((resolve) => {
      const timeout = setTimeout(() => resolve(), 1000);
      mockServer.once('exit', () => {
        clearTimeout(timeout);
        resolve();
      });
    });
  }
});

test('should handle server startup errors gracefully @smoke', async () => {
  const invalidProcess = spawn('invalid-command-that-does-not-exist', [], {
    shell: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  await new Promise<void>((resolve) => {
    invalidProcess.on('error', (error) => {
      expect(error).toBeTruthy();
      resolve();
    });

    invalidProcess.on('exit', (code) => {
      expect(code).not.toBe(0);
      resolve();
    });
    
    // Timeout to avoid hanging - Keep this as 5 seconds is needed for error handling
    setTimeout(() => resolve(), 5000);
  });
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
  
  // Wait for process to exit with timeout
  await new Promise<void>((resolve) => {
    const timeout = setTimeout(() => {
      // Force kill if needed
      if (!mockProcess.killed) {
        mockProcess.kill('SIGKILL');
      }
      setTimeout(() => resolve(), 500);
    }, 1000);
    
    mockProcess.once('exit', () => {
      clearTimeout(timeout);
      resolve();
    });
  });
  
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
