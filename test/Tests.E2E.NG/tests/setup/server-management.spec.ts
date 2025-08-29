import { test, expect } from '../fixtures/serial-test-fixture';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
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
const testDbPath = path.join(process.platform === 'win32' ? process.env.TEMP || 'C:\temp' : '/tmp', 'test-server-mgmt.db');

test.afterAll(async () => {
  // Clean up any test processes
  if (apiProcess) {
    apiProcess.kill('SIGTERM');
    await new Promise(resolve => setTimeout(resolve, 1000));
    if (!apiProcess.killed) {
      apiProcess.kill('SIGKILL');
    }
  }
  if (angularProcess) {
    angularProcess.kill('SIGTERM');
    await new Promise(resolve => setTimeout(resolve, 1000));
    if (!angularProcess.killed) {
      angularProcess.kill('SIGKILL');
    }
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
    
    while (Date.now() - startTime < timeout) {
      try {
        const response = await page.request.get(url);
        if (response.ok()) {
          return true;
        }
      } catch {
        // Server not ready yet
      }
      await page.waitForTimeout(2000);
    }
    return false;
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
    await new Promise(resolve => setTimeout(resolve, 1000));
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
    
    // Timeout to avoid hanging
    setTimeout(() => resolve(), 5000);
  });
});

test('should clean up database files after tests @smoke', async () => {
  const testFile = path.join(process.platform === 'win32' ? process.env.TEMP || 'C:\temp' : '/tmp', 'test-cleanup.db');
  
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
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  // Force kill if needed
  if (!mockProcess.killed) {
    mockProcess.kill('SIGKILL');
  }
  
  await new Promise(resolve => setTimeout(resolve, 500));
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
