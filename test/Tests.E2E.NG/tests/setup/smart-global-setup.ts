import { FullConfig } from '@playwright/test';
import { spawn, ChildProcess, execSync } from 'child_process';
import * as path from 'path';
import * as fs from 'fs/promises';
import fetch from 'node-fetch';

let apiServerProcess: ChildProcess | null = null;
let angularServerProcess: ChildProcess | null = null;

// Check if running on Windows
const isWindows = process.platform === 'win32';

/**
 * Check if a server is already running on a port
 */
async function isServerRunning(url: string): Promise<boolean> {
  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: { 'Accept': 'application/json' },
      signal: AbortSignal.timeout(2000)
    });
    return response.ok;
  } catch {
    return false;
  }
}

/**
 * Wait for server readiness
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
    console.log('🗄️ Resetting database...');
    const response = await fetch(`${apiUrl}/api/database/reset`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ preserveSchema: true }),
      signal: AbortSignal.timeout(5000)
    });
    
    if (response.ok) {
      console.log('✅ Database reset successfully');
      return true;
    } else {
      console.warn(`⚠️ Database reset returned ${response.status}`);
      return false;
    }
  } catch (error: any) {
    console.warn(`⚠️ Could not reset database: ${error.message}`);
    return false;
  }
}

/**
 * Smart Global Setup - Reuses existing servers when possible
 * Only starts servers if they're not already running
 * Always creates a fresh database for test isolation
 */
async function smartGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting smart test setup...');
  
  const apiPort = process.env.API_PORT || '5172';
  const angularPort = process.env.ANGULAR_PORT || '4200';
  const apiUrl = `http://localhost:${apiPort}`;
  const angularUrl = `http://localhost:${angularPort}`;
  
  // Check if servers are already running
  const apiRunning = await isServerRunning(`${apiUrl}/health`);
  const angularRunning = await isServerRunning(angularUrl);
  
  console.log(`📦 Server Status:`);
  console.log(`   API (${apiPort}): ${apiRunning ? '✅ Already running' : '🔴 Not running'}`);
  console.log(`   Angular (${angularPort}): ${angularRunning ? '✅ Already running' : '🔴 Not running'}`);
  
  // Create unique database for this test run
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const databasePath = path.join(tempDir, `CrudTest_${timestamp}.db`);
  
  // Start API server if not running
  if (!apiRunning) {
    console.log('🚀 Starting API server...');
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
    
    // Wait for API to be ready
    const apiReady = await waitForServer(`${apiUrl}/health`, 30000);
    if (!apiReady) {
      throw new Error('API server failed to start within 30 seconds');
    }
    console.log('✅ API server started successfully');
  } else {
    console.log('♻️ Reusing existing API server');
    
    // Update environment to use existing database
    process.env.ConnectionStrings__DefaultConnection = `Data Source=${databasePath}`;
    
    // Reset database via API
    await resetDatabase(apiUrl);
  }
  
  // Start Angular server if not running
  if (!angularRunning) {
    console.log('🚀 Starting Angular server (this may take a minute)...');
    const angularProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Angular');
    
    angularServerProcess = spawn('npm', ['start'], {
      cwd: angularProjectPath,
      env: {
        ...process.env,
        'API_URL': apiUrl,
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
    
    // Wait for Angular (longer timeout for compilation)
    console.log('⏳ Waiting for Angular to compile...');
    const angularReady = await waitForServer(angularUrl, 90000);
    if (!angularReady) {
      throw new Error('Angular server failed to start within 90 seconds');
    }
    console.log('✅ Angular server started successfully');
  } else {
    console.log('♻️ Reusing existing Angular server');
  }
  
  // Store configuration for tests
  process.env.API_URL = apiUrl;
  process.env.ANGULAR_URL = angularUrl;
  process.env.DATABASE_PATH = databasePath;
  
  console.log('✅ Smart test setup completed');
  console.log('💡 Tip: Servers will remain running for faster subsequent test runs');
  
  // Return teardown function
  return async () => {
    console.log('🧹 Test cleanup...');
    
    // Only kill servers if we started them
    if (apiServerProcess) {
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
      console.log('♻️ Leaving API server running (was already running)');
    }
    
    if (angularServerProcess) {
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
      console.log('♻️ Leaving Angular server running (was already running)');
    }
    
    // Always clean up test database
    try {
      await fs.unlink(databasePath);
      console.log('🗑️ Cleaned up test database');
    } catch {
      // Ignore if already deleted
    }
    
    console.log('✅ Cleanup completed');
  };
}

export default smartGlobalSetup;