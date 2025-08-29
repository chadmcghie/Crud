import { FullConfig } from '@playwright/test';
import { spawn, ChildProcess, execSync } from 'child_process';
import * as path from 'path';
import * as fs from 'fs/promises';
import fetch from 'node-fetch';
import { ServerStatus, isServerRunning, checkPortAvailability } from './server-utils';

// Track server processes
let apiServerProcess: ChildProcess | null = null;
let angularServerProcess: ChildProcess | null = null;
let serverStatus: ServerStatus | null = null;

// Check if running on Windows
const isWindows = process.platform === 'win32';

/**
 * Wait for server to be ready with retries
 */
async function waitForServer(url: string, timeout: number = 30000): Promise<boolean> {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    if (await isServerRunning(url)) {
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  
  return false;
}

/**
 * Reset database via API endpoint
 */
async function resetDatabase(apiUrl: string): Promise<boolean> {
  try {
    console.log('🗄️  Resetting database for clean test environment...');
    
    const response = await fetch(`${apiUrl}/api/database/reset`, {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
      },
      body: JSON.stringify({ 
        preserveSchema: true,
        workerIndex: 0
      }),
      signal: AbortSignal.timeout(5000)
    });
    
    if (response.ok) {
      console.log('✅ Database reset successfully');
      return true;
    } else {
      console.warn(`⚠️  Database reset returned ${response.status}`);
      return false;
    }
  } catch (error: any) {
    console.warn(`⚠️  Could not reset database: ${error.message}`);
    return false;
  }
}

/**
 * Optimized Global Setup with Smart Server Detection
 * - Detects and reuses existing servers
 * - Only starts servers if not already running
 * - Provides clear status feedback
 */
async function optimizedGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting optimized test setup with server detection...');
  
  const apiPort = process.env.API_PORT || '5172';
  const angularPort = process.env.ANGULAR_PORT || '4200';
  const apiUrl = `http://localhost:${apiPort}`;
  const angularUrl = `http://localhost:${angularPort}`;
  
  // Initialize server status tracker
  serverStatus = new ServerStatus();
  
  // Check current server status
  console.log('\n🔍 Detecting existing servers...');
  await serverStatus.checkAll(apiUrl, angularUrl);
  console.log(serverStatus.getStatusReport());
  
  const apiInfo = serverStatus.getServerInfo('api')!;
  const angularInfo = serverStatus.getServerInfo('angular')!;
  
  // Create unique database for this test run
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const databasePath = path.join(tempDir, `CrudTest_${timestamp}.db`);
  
  console.log(`\n📊 Test Configuration:`);
  console.log(`   Database: ${path.basename(databasePath)}`);
  
  // Handle API server
  if (!apiInfo.running) {
    console.log('\n🚀 Starting API server (not currently running)...');
    const apiProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Api');
    
    const dotnetArgs = process.env.CI ? ['run', '--no-build', '--configuration', 'Release'] : ['run'];
    
    apiServerProcess = spawn('dotnet', dotnetArgs, {
      cwd: apiProjectPath,
      env: {
        ...process.env,
        'ASPNETCORE_URLS': apiUrl,
        'ASPNETCORE_ENVIRONMENT': 'Testing',
        'ConnectionStrings__DefaultConnection': `Data Source=${databasePath}`,
        'DatabaseProvider': 'SQLite',
        'Logging__LogLevel__Default': 'Warning',
        'Logging__LogLevel__Microsoft': 'Warning',
        'Logging__LogLevel__Microsoft.EntityFrameworkCore': 'Warning',
        'TEST_RESET_TOKEN': 'test-only-token'
      },
      shell: isWindows,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    
    apiServerProcess.stderr?.on('data', (data) => {
      const message = data.toString();
      if (message.includes('Error') || message.includes('Exception')) {
        console.error(`[API Error] ${message}`);
      }
    });
    
    apiServerProcess.on('error', (error) => {
      console.error('❌ Failed to start API server:', error);
      throw error;
    });
    
    // Mark as started by us
    if (apiServerProcess.pid) {
      serverStatus.markAsStartedByUs('api', apiServerProcess.pid);
    }
    
    // Wait for API to be ready
    const apiReady = await waitForServer(`${apiUrl}/health`, 30000);
    if (!apiReady) {
      throw new Error('API server failed to start within 30 seconds');
    }
    console.log('✅ API server started successfully');
  } else {
    console.log('\n♻️  Reusing existing API server - no startup delay!');
    serverStatus.markAsPreExisting('api');
    
    // Update environment to use existing database
    process.env.ConnectionStrings__DefaultConnection = `Data Source=${databasePath}`;
    
    // Reset database for clean test state
    const resetSuccessful = await resetDatabase(apiUrl);
    if (!resetSuccessful) {
      console.warn('⚠️  Database reset failed, tests may see stale data');
    }
  }
  
  // Handle Angular server
  if (!angularInfo.running) {
    console.log('\n🚀 Starting Angular server (this may take 60-90 seconds)...');
    const angularProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Angular');
    
    angularServerProcess = spawn('npm', ['start'], {
      cwd: angularProjectPath,
      env: {
        ...process.env,
        'API_URL': apiUrl,
        'PORT': angularPort,
      },
      shell: isWindows,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    
    angularServerProcess.stderr?.on('data', (data) => {
      const message = data.toString();
      if (message.includes('Error') || message.includes('ERROR')) {
        console.error(`[Angular Error] ${message}`);
      }
    });
    
    angularServerProcess.on('error', (error) => {
      console.error('❌ Failed to start Angular server:', error);
      throw error;
    });
    
    // Mark as started by us
    if (angularServerProcess.pid) {
      serverStatus.markAsStartedByUs('angular', angularServerProcess.pid);
    }
    
    // Wait for Angular (longer timeout for compilation)
    console.log('⏳ Waiting for Angular to compile...');
    const angularReady = await waitForServer(angularUrl, 90000);
    if (!angularReady) {
      throw new Error('Angular server failed to start within 90 seconds');
    }
    console.log('✅ Angular server started successfully');
  } else {
    console.log('\n♻️  Reusing existing Angular server - saving 60-90 seconds!');
    serverStatus.markAsPreExisting('angular');
  }
  
  // Store configuration for tests
  process.env.API_URL = apiUrl;
  process.env.ANGULAR_URL = angularUrl;
  process.env.DATABASE_PATH = databasePath;
  
  // Calculate time saved
  const timeSaved = (apiInfo.running ? 10 : 0) + (angularInfo.running ? 90 : 0);
  if (timeSaved > 0) {
    console.log(`\n⚡ Time saved by reusing servers: ~${timeSaved} seconds`);
  }
  
  console.log('\n✅ Test setup completed - ready to run tests!');
  console.log('💡 Tip: Keep servers running between test runs for fastest execution\n');
  
  // Return teardown function
  return async () => {
    console.log('\n🧹 Test cleanup starting...');
    
    // Only kill servers that we started
    if (serverStatus?.shouldKillOnTeardown('api') && apiServerProcess) {
      console.log('🛑 Stopping API server (started by this test run)...');
      if (isWindows && apiServerProcess.pid) {
        try {
          execSync(`taskkill /F /PID ${apiServerProcess.pid} /T`, { stdio: 'ignore' });
        } catch {
          // Process might already be dead
        }
      } else {
        apiServerProcess.kill('SIGTERM');
      }
    } else {
      console.log('♻️  Leaving API server running for next test run');
    }
    
    if (serverStatus?.shouldKillOnTeardown('angular') && angularServerProcess) {
      console.log('🛑 Stopping Angular server (started by this test run)...');
      if (isWindows && angularServerProcess.pid) {
        try {
          execSync(`taskkill /F /PID ${angularServerProcess.pid} /T`, { stdio: 'ignore' });
        } catch {
          // Process might already be dead
        }
      } else {
        angularServerProcess.kill('SIGTERM');
      }
    } else {
      console.log('♻️  Leaving Angular server running for next test run');
    }
    
    // Always clean up test database
    try {
      await fs.unlink(databasePath);
      console.log('🗑️  Cleaned up test database');
    } catch {
      // Ignore if already deleted
    }
    
    console.log('✅ Cleanup completed\n');
  };
}

export default optimizedGlobalSetup;