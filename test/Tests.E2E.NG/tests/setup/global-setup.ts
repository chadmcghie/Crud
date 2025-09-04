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
 * Kill any process using the specified port
 */
function killProcessOnPort(port: string): void {
  try {
    if (isWindows) {
      // Windows: Use netstat and taskkill
      try {
        const result = execSync(`netstat -ano | findstr :${port}`, { encoding: 'utf8' });
        const lines = result.split('\n');
        const pids = new Set<string>();
        
        for (const line of lines) {
          const parts = line.trim().split(/\s+/);
          const pid = parts[parts.length - 1];
          if (pid && pid !== '0') {
            pids.add(pid);
          }
        }
        
        for (const pid of pids) {
          try {
            execSync(`taskkill /F /PID ${pid}`, { stdio: 'ignore' });
            console.log(`  Killed process ${pid} on port ${port}`);
          } catch {
            // Process might already be dead
          }
        }
      } catch {
        // No process found on port
      }
    } else {
      // Unix/Linux/Mac: Use lsof
      try {
        const result = execSync(`lsof -ti:${port}`, { encoding: 'utf8' });
        const pids = result.trim().split('\n');
        for (const pid of pids) {
          if (pid) {
            execSync(`kill -9 ${pid}`, { stdio: 'ignore' });
            console.log(`  Killed process ${pid} on port ${port}`);
          }
        }
      } catch {
        // No process found on port
      }
    }
  } catch (error) {
    // Ignore errors - port might be free
  }
}

/**
 * Enhanced wait for server readiness with retry logic
 */
async function waitForServer(url: string, timeout: number = 30000): Promise<boolean> {
  const startTime = Date.now();
  let lastError: any = null;
  
  while (Date.now() - startTime < timeout) {
    try {
      const response = await fetch(url, {
        method: 'GET',
        headers: { 'Accept': 'application/json' },
        // Short timeout for each attempt
        signal: AbortSignal.timeout(5000)
      });
      
      if (response.ok) {
        // Double-check with a second request to ensure stability
        await new Promise(resolve => setTimeout(resolve, 500));
        const verifyResponse = await fetch(url, {
          signal: AbortSignal.timeout(5000)
        });
        
        if (verifyResponse.ok) {
          return true;
        }
      }
    } catch (error) {
      lastError = error;
      // Server not ready yet
    }
    
    // Wait before next attempt
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
  
  console.warn(`Server at ${url} did not become ready: ${lastError?.message || 'Unknown error'}`);
  return false;
}

/**
 * Robust Global Setup with Port Management
 * Kills any existing processes on required ports before starting new ones
 */
async function robustGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting robust test setup...');
  
  const apiPort = process.env.API_PORT || '5172';
  const angularPort = process.env.ANGULAR_PORT || '4200';
  
  // Kill any existing processes on our ports
  console.log('🧹 Cleaning up existing processes...');
  killProcessOnPort(apiPort);
  killProcessOnPort(angularPort);
  
  // Wait a moment for ports to be fully released
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Simple database path - unique per test run
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const databasePath = path.join(tempDir, `CrudTest_${timestamp}.db`);
  
  console.log(`📦 Configuration:`);
  console.log(`   API Port: ${apiPort}`);
  console.log(`   Angular Port: ${angularPort}`);
  console.log(`   Database: ${databasePath}`);
  
  // Start API server
  console.log('🚀 Starting API server...');
  const apiProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Api');
  
  // Use --no-build only in CI where we pre-build, otherwise allow building
  const dotnetArgs = process.env.CI ? ['run', '--no-build', '--configuration', 'Release'] : ['run'];
  
  apiServerProcess = spawn('dotnet', dotnetArgs, {
    cwd: apiProjectPath,
    env: {
      ...process.env,
      'ASPNETCORE_URLS': `http://localhost:${apiPort}`,
      'ASPNETCORE_ENVIRONMENT': 'Testing',
      'ConnectionStrings__DefaultConnection': `Data Source=${databasePath}`,
      'DatabaseProvider': 'SQLite',
      'Logging__LogLevel__Default': 'Error', // Only log errors to reduce noise
      'Logging__LogLevel__Microsoft': 'Error',
      'Logging__LogLevel__Microsoft.EntityFrameworkCore': 'Error',
    },
    shell: isWindows, // Only use shell on Windows
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  
  // Only log errors, not all output
  apiServerProcess.stderr?.on('data', (data) => {
    const message = data.toString();
    // Only show actual errors, not info messages
    if (message.includes('Error') || message.includes('Exception')) {
      console.error(`[API Error] ${message}`);
    }
  });
  
  apiServerProcess.on('error', (error) => {
    console.error('❌ Failed to start API server:', error);
    throw error;
  });
  
  // Wait for API server health endpoint
  const apiReady = await waitForServer(`http://localhost:${apiPort}/health`, 30000);
  if (!apiReady) {
    throw new Error('API server failed to start within 30 seconds');
  }
  
  // Additional verification: Check API endpoints are responding
  try {
    const endpointsToCheck = ['/api/people', '/api/roles'];
    for (const endpoint of endpointsToCheck) {
      const response = await fetch(`http://localhost:${apiPort}${endpoint}`, {
        signal: AbortSignal.timeout(5000)
      });
      if (!response.ok && response.status !== 404) {
        console.warn(`API endpoint ${endpoint} returned ${response.status}`);
      }
    }
  } catch (error: any) {
    console.warn(`API endpoint verification warning: ${error.message}`);
  }
  
  console.log('✅ API server is ready and responding');
  
  // Start Angular server
  console.log('🚀 Starting Angular server...');
  const angularProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Angular');
  
  // Use npm start which is already configured properly
  angularServerProcess = spawn('npm', ['start'], {
    cwd: angularProjectPath,
    env: {
      ...process.env,
      'API_URL': `http://localhost:${apiPort}`,
    },
    shell: isWindows, // Only use shell on Windows
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  
  // Only log errors, not all output
  angularServerProcess.stderr?.on('data', (data) => {
    const message = data.toString();
    // Only show actual errors, not webpack progress
    if (message.includes('Error') || message.includes('ERROR')) {
      console.error(`[Angular Error] ${message}`);
    }
  });
  
  angularServerProcess.on('error', (error) => {
    console.error('❌ Failed to start Angular server:', error);
    throw error;
  });
  
  // Wait for Angular server (takes longer due to compilation)
  console.log('⏳ Waiting for Angular to compile and start (this may take a minute)...');
  const angularReady = await waitForServer(`http://localhost:${angularPort}`, 90000);
  if (!angularReady) {
    throw new Error('Angular server failed to start within 90 seconds');
  }
  console.log('✅ Angular server is ready');
  
  // Store configuration for tests
  process.env.API_URL = `http://localhost:${apiPort}`;
  process.env.ANGULAR_URL = `http://localhost:${angularPort}`;
  process.env.DATABASE_PATH = databasePath;
  
  console.log('✅ Robust test setup completed');
  
  // Give servers a moment to stabilize before tests start
  console.log('⏳ Waiting for servers to stabilize...');
  await new Promise(resolve => setTimeout(resolve, 3000));
  console.log('✅ Ready for tests');
  
  // Return teardown function
  return async () => {
    console.log('🛑 Shutting down test servers...');
    
    // Kill API server
    if (apiServerProcess) {
      if (isWindows && apiServerProcess.pid) {
        try {
          execSync(`taskkill /F /PID ${apiServerProcess.pid} /T`, { stdio: 'ignore' });
        } catch {
          // Process might already be dead
        }
      } else {
        apiServerProcess.kill('SIGTERM');
        await new Promise(resolve => setTimeout(resolve, 2000));
        if (!apiServerProcess.killed) {
          apiServerProcess.kill('SIGKILL');
        }
      }
    }
    
    // Kill Angular server
    if (angularServerProcess) {
      if (isWindows && angularServerProcess.pid) {
        try {
          execSync(`taskkill /F /PID ${angularServerProcess.pid} /T`, { stdio: 'ignore' });
        } catch {
          // Process might already be dead
        }
      } else {
        angularServerProcess.kill('SIGTERM');
        await new Promise(resolve => setTimeout(resolve, 2000));
        if (!angularServerProcess.killed) {
          angularServerProcess.kill('SIGKILL');
        }
      }
    }
    
    // Also kill any processes still on the ports (cleanup insurance)
    killProcessOnPort(apiPort);
    killProcessOnPort(angularPort);
    
    // Clean up database
    try {
      await fs.unlink(databasePath);
      console.log('🗑️ Cleaned up test database');
    } catch {
      // Ignore if already deleted
    }
    
    console.log('✅ Robust teardown completed');
  };
}

export default robustGlobalSetup;