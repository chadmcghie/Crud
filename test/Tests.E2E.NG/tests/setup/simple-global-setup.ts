import { FullConfig } from '@playwright/test';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs/promises';
import fetch from 'node-fetch';

let apiServerProcess: ChildProcess | null = null;
let angularServerProcess: ChildProcess | null = null;

// Check if running on Windows
const isWindows = process.platform === 'win32';

/**
 * Simple wait for server readiness
 */
async function waitForServer(url: string, timeout: number = 30000): Promise<boolean> {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        return true;
      }
    } catch {
      // Server not ready yet
    }
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
  
  return false;
}

/**
 * Simple Global Setup
 * Starts API and Angular servers using basic spawn
 * No persistence, no lock files, no complex state management
 */
async function simpleGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting simple test setup...');
  
  const apiPort = process.env.API_PORT || '5172';
  const angularPort = process.env.ANGULAR_PORT || '4200';
  
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
    console.error(`[API Error] ${data.toString()}`);
  });
  
  apiServerProcess.on('error', (error) => {
    console.error('❌ Failed to start API server:', error);
    throw error;
  });
  
  // Wait for API server
  const apiReady = await waitForServer(`http://localhost:${apiPort}/health`, 30000);
  if (!apiReady) {
    throw new Error('API server failed to start within 30 seconds');
  }
  console.log('✅ API server is ready');
  
  // Start Angular server
  console.log('🚀 Starting Angular server...');
  const angularProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Angular');
  
  angularServerProcess = spawn('npm', ['run', 'start', '--', '--port', angularPort], {
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
    console.error(`[Angular Error] ${data.toString()}`);
  });
  
  angularServerProcess.on('error', (error) => {
    console.error('❌ Failed to start Angular server:', error);
    throw error;
  });
  
  // Wait for Angular server
  const angularReady = await waitForServer(`http://localhost:${angularPort}`, 45000);
  if (!angularReady) {
    throw new Error('Angular server failed to start within 45 seconds');
  }
  console.log('✅ Angular server is ready');
  
  // Store configuration for tests
  process.env.API_URL = `http://localhost:${apiPort}`;
  process.env.ANGULAR_URL = `http://localhost:${angularPort}`;
  process.env.DATABASE_PATH = databasePath;
  
  console.log('✅ Simple test setup completed');
  
  // Return teardown function
  return async () => {
    console.log('🛑 Shutting down test servers...');
    
    // Kill API server
    if (apiServerProcess) {
      apiServerProcess.kill('SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 2000));
      if (!apiServerProcess.killed) {
        apiServerProcess.kill('SIGKILL');
      }
    }
    
    // Kill Angular server
    if (angularServerProcess) {
      angularServerProcess.kill('SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 2000));
      if (!angularServerProcess.killed) {
        angularServerProcess.kill('SIGKILL');
      }
    }
    
    // Clean up database
    try {
      await fs.unlink(databasePath);
      console.log('🗑️ Cleaned up test database');
    } catch {
      // Ignore if already deleted
    }
    
    console.log('✅ Simple teardown completed');
  };
}

export default simpleGlobalSetup;