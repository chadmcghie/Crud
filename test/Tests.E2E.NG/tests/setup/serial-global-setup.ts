import { FullConfig } from '@playwright/test';
import { spawn, ChildProcess } from 'child_process';
import * as path from 'path';
import * as fs from 'fs/promises';
import { checkPortsAvailable, killProcessOnPort, waitForServer } from './port-utils';

let apiServerProcess: ChildProcess | null = null;
let angularServerProcess: ChildProcess | null = null;

/**
 * Serial Global Setup
 * Starts a single set of servers that will be shared by all tests
 * Database is cleaned between tests, not between workers
 */
async function serialGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting serial test setup...');
  console.log('📋 Strategy: Single worker, shared servers, clean database per test');
  
  const apiPort = process.env.API_PORT || '5172';
  const angularPort = process.env.ANGULAR_PORT || '4200';
  
  // Set up database path
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const databasePath = path.join(tempDir, `CrudTest_Serial_${timestamp}.db`);
  
  console.log(`📦 Configuration:`);
  console.log(`   API Port: ${apiPort}`);
  console.log(`   Angular Port: ${angularPort}`);
  console.log(`   Database: ${databasePath}`);
  
  // Clean up any existing test databases
  try {
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('CrudTest_'));
    for (const db of testDbs) {
      try {
        await fs.unlink(path.join(tempDir, db));
        console.log(`🗑️ Cleaned up old test database: ${db}`);
      } catch (err) {
        // Ignore errors - file might be in use
      }
    }
  } catch (err) {
    console.warn('⚠️ Could not clean up old test databases:', err);
  }
  
  // Check ports availability
  const portCheck = await checkPortsAvailable([parseInt(apiPort), parseInt(angularPort)]);
  if (!portCheck.available) {
    console.log(`⚠️ Ports in use: ${portCheck.conflicts.join(', ')}`);
    console.log('🔪 Attempting to free up ports...');
    
    for (const port of portCheck.conflicts) {
      await killProcessOnPort(port);
    }
    
    // Wait a bit for ports to be released
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
  
  // Start API server
  console.log('🚀 Starting API server...');
  const apiEnv = {
    ...process.env,
    'ASPNETCORE_URLS': `http://localhost:${apiPort}`,
    'ASPNETCORE_ENVIRONMENT': 'Testing',
    'DatabasePath': databasePath,
    'ConnectionStrings__DefaultConnection': `Data Source=${databasePath}`,
    'Logging__LogLevel__Default': 'Warning',
    'Logging__LogLevel__Microsoft': 'Warning',
    'Logging__LogLevel__Microsoft.EntityFrameworkCore': 'Warning',
  };
  
  const apiProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Api');
  apiServerProcess = spawn('dotnet', ['run', '--no-build'], {
    cwd: apiProjectPath,
    env: apiEnv,
    shell: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  
  // Capture API server output for debugging
  apiServerProcess.stdout?.on('data', (data) => {
    if (process.env.DEBUG_SERVERS) {
      console.log(`[API] ${data.toString()}`);
    }
  });
  
  apiServerProcess.stderr?.on('data', (data) => {
    console.error(`[API ERROR] ${data.toString()}`);
  });
  
  apiServerProcess.on('error', (error) => {
    console.error('❌ Failed to start API server:', error);
    throw error;
  });
  
  // Wait for API server to be ready
  const apiReady = await waitForServer(`http://localhost:${apiPort}/health`, 30000);
  if (!apiReady) {
    throw new Error('API server failed to start within 30 seconds');
  }
  console.log('✅ API server is ready');
  
  // Start Angular server
  console.log('🚀 Starting Angular server...');
  const angularEnv = {
    ...process.env,
    'API_URL': `http://localhost:${apiPort}`,
  };
  
  const angularProjectPath = path.join(process.cwd(), '..', '..', 'src', 'Angular');
  angularServerProcess = spawn('npm', ['run', 'serve:test', '--', '--port', angularPort], {
    cwd: angularProjectPath,
    env: angularEnv,
    shell: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  
  // Capture Angular server output for debugging
  angularServerProcess.stdout?.on('data', (data) => {
    if (process.env.DEBUG_SERVERS) {
      console.log(`[Angular] ${data.toString()}`);
    }
  });
  
  angularServerProcess.stderr?.on('data', (data) => {
    const message = data.toString();
    // Ignore common Angular CLI warnings
    if (!message.includes('Warning:') && !message.includes('DeprecationWarning')) {
      console.error(`[Angular ERROR] ${message}`);
    }
  });
  
  angularServerProcess.on('error', (error) => {
    console.error('❌ Failed to start Angular server:', error);
    throw error;
  });
  
  // Wait for Angular server to be ready
  const angularReady = await waitForServer(`http://localhost:${angularPort}`, 45000);
  if (!angularReady) {
    throw new Error('Angular server failed to start within 45 seconds');
  }
  console.log('✅ Angular server is ready');
  
  // Store configuration for tests
  process.env.API_URL = `http://localhost:${apiPort}`;
  process.env.ANGULAR_URL = `http://localhost:${angularPort}`;
  process.env.DATABASE_PATH = databasePath;
  process.env.SERIAL_MODE = 'true';
  
  // Store process IDs for cleanup
  if (apiServerProcess.pid) {
    process.env.API_SERVER_PID = apiServerProcess.pid.toString();
  }
  if (angularServerProcess.pid) {
    process.env.ANGULAR_SERVER_PID = angularServerProcess.pid.toString();
  }
  
  console.log('✅ Serial test setup completed');
  console.log('📋 Servers will remain running for all tests');
  console.log('🗄️ Database will be cleaned between test files');
  
  return async () => {
    // This teardown function will be called automatically
    console.log('🛑 Shutting down test servers...');
    
    if (apiServerProcess) {
      apiServerProcess.kill('SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 2000));
      if (!apiServerProcess.killed) {
        apiServerProcess.kill('SIGKILL');
      }
    }
    
    if (angularServerProcess) {
      angularServerProcess.kill('SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 2000));
      if (!angularServerProcess.killed) {
        angularServerProcess.kill('SIGKILL');
      }
    }
    
    // Clean up database
    try {
      if (process.env.DATABASE_PATH) {
        await fs.unlink(process.env.DATABASE_PATH);
        console.log('🗑️ Cleaned up test database');
      }
    } catch (err) {
      console.warn('⚠️ Could not clean up test database:', err);
    }
    
    console.log('✅ Test cleanup completed');
  };
}

export default serialGlobalSetup;