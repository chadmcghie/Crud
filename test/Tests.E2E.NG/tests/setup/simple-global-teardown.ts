import { FullConfig } from '@playwright/test';
import * as fs from 'fs/promises';
import * as path from 'path';

/**
 * Simple Global Teardown
 * Basic cleanup with no complex state management
 * Just clean up database files and ensure ports are free
 */
async function simpleGlobalTeardown(config: FullConfig) {
  console.log('🧹 Starting simple global teardown...');
  
  // Clean up test database if it exists
  if (process.env.DATABASE_PATH) {
    try {
      await fs.unlink(process.env.DATABASE_PATH);
      console.log('🗑️ Cleaned up test database');
    } catch (err) {
      console.warn('⚠️ Could not clean up test database:', err);
    }
  }
  
  // Clean up any leftover test databases in temp directory
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  try {
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('CrudTest_'));
    
    for (const db of testDbs) {
      try {
        const dbPath = path.join(tempDir, db);
        const stats = await fs.stat(dbPath);
        
        // Only delete files older than 1 hour to avoid conflicts
        const oneHourAgo = Date.now() - (60 * 60 * 1000);
        if (stats.mtimeMs < oneHourAgo) {
          await fs.unlink(dbPath);
          console.log(`🗑️ Cleaned up old test database: ${db}`);
        }
      } catch {
        // Ignore errors - file might be in use
      }
    }
  } catch {
    // Ignore if temp directory is not accessible
  }
  
  // Simple port cleanup as fallback
  // Note: Actual process killing is handled by the teardown function returned from setup
  const ports = [
    parseInt(process.env.API_PORT || '5172'),
    parseInt(process.env.ANGULAR_PORT || '4200')
  ];
  
  if (process.platform === 'win32') {
    // Windows: Use netstat to check if ports are still in use
    const { exec } = require('child_process');
    for (const port of ports) {
      try {
        await new Promise<void>((resolve) => {
          exec(`netstat -ano | findstr :${port}`, (error: any, stdout: string) => {
            if (!error && stdout) {
              console.log(`⚠️ Port ${port} may still be in use`);
            }
            resolve();
          });
        });
      } catch {
        // Ignore errors
      }
    }
  } else {
    // Unix-like: Use lsof to check if ports are still in use
    const { exec } = require('child_process');
    for (const port of ports) {
      try {
        await new Promise<void>((resolve) => {
          exec(`lsof -i :${port}`, (error: any, stdout: string) => {
            if (!error && stdout) {
              console.log(`⚠️ Port ${port} may still be in use`);
            }
            resolve();
          });
        });
      } catch {
        // Ignore errors
      }
    }
  }
  
  console.log('✅ Simple global teardown completed');
}

export default simpleGlobalTeardown;