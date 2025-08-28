import { FullConfig } from '@playwright/test';
import * as fs from 'fs/promises';
import * as path from 'path';
import { killProcessOnPort } from './port-utils';

/**
 * Serial Global Teardown
 * Ensures all servers are stopped and databases are cleaned up
 */
async function serialGlobalTeardown(config: FullConfig) {
  console.log('🛑 Starting global teardown...');
  
  // Kill servers by PID if available
  if (process.env.API_SERVER_PID) {
    try {
      process.kill(parseInt(process.env.API_SERVER_PID), 'SIGTERM');
      console.log('✅ Stopped API server');
    } catch (err) {
      console.warn('⚠️ Could not stop API server by PID:', err);
    }
  }
  
  if (process.env.ANGULAR_SERVER_PID) {
    try {
      process.kill(parseInt(process.env.ANGULAR_SERVER_PID), 'SIGTERM');
      console.log('✅ Stopped Angular server');
    } catch (err) {
      console.warn('⚠️ Could not stop Angular server by PID:', err);
    }
  }
  
  // Kill by port as fallback
  const apiPort = parseInt(process.env.API_PORT || '5172');
  const angularPort = parseInt(process.env.ANGULAR_PORT || '4200');
  
  await killProcessOnPort(apiPort);
  await killProcessOnPort(angularPort);
  
  // Clean up test database
  if (process.env.DATABASE_PATH) {
    try {
      // Try multiple times in case file is still locked
      for (let i = 0; i < 3; i++) {
        try {
          await fs.unlink(process.env.DATABASE_PATH);
          console.log('🗑️ Cleaned up test database');
          break;
        } catch (err) {
          if (i === 2) throw err;
          await new Promise(resolve => setTimeout(resolve, 1000));
        }
      }
    } catch (err) {
      console.warn('⚠️ Could not clean up test database:', err);
    }
  }
  
  // Clean up any orphaned test databases
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  try {
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('CrudTest_'));
    
    for (const db of testDbs) {
      try {
        const filePath = path.join(tempDir, db);
        const stats = await fs.stat(filePath);
        const ageInHours = (Date.now() - stats.mtime.getTime()) / (1000 * 60 * 60);
        
        // Clean up databases older than 1 hour
        if (ageInHours > 1) {
          await fs.unlink(filePath);
          console.log(`🗑️ Cleaned up orphaned test database: ${db}`);
        }
      } catch (err) {
        // Ignore errors
      }
    }
  } catch (err) {
    // Ignore errors
  }
  
  console.log('✅ Global teardown completed');
}

export default serialGlobalTeardown;