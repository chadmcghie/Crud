import { FullConfig } from '@playwright/test';
import fs from 'fs/promises';
import path from 'path';
import { killAllTestServers } from './port-utils';
import { PersistentServerManager } from './persistent-server-manager';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global test teardown...');
  
  // Stop all persistent servers
  await PersistentServerManager.cleanupAll();
  
  // Kill any remaining test servers as a safety measure
  try {
    await killAllTestServers();
  } catch (error) {
    console.warn('⚠️ Error during server cleanup:', error);
  }
  
  const workers = config.workers || 1;
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  
  // Clean up databases for parallel workers
  for (let i = 0; i < workers; i++) {
    // Clean up parallel worker databases
    try {
      const files = await fs.readdir(tempDir);
      const parallelDbFiles = files.filter(f => f.startsWith(`CrudTest_Parallel${i}_`) && f.endsWith('.db'));
      
      for (const file of parallelDbFiles) {
        const dbPath = path.join(tempDir, file);
        await fs.unlink(dbPath);
        console.log(`🗑️ Deleted database for parallel worker ${i}: ${file}`);
      }
    } catch (error) {
      console.log(`ℹ️ Database for parallel worker ${i} already cleaned up or doesn't exist`);
    }
    
    // Also clean up old worker databases (for backward compatibility)
    const workerDatabase = process.env[`WORKER_${i}_DATABASE`];
    if (workerDatabase) {
      try {
        await fs.access(workerDatabase);
        await fs.unlink(workerDatabase);
        console.log(`🗑️ Cleaned up old database for worker ${i}: ${path.basename(workerDatabase)}`);
      } catch (error) {
        // File doesn't exist or already cleaned up
      }
    }
  }
  
  // Clean up any remaining test database files in temp directory
  try {
    const files = await fs.readdir(tempDir);
    const testDbFiles = files.filter(file => file.startsWith('CrudTest_') && file.endsWith('.db'));
    
    if (testDbFiles.length > 0) {
      console.log(`🧹 Found ${testDbFiles.length} leftover test database(s), cleaning up...`);
      for (const file of testDbFiles) {
        try {
          await fs.unlink(path.join(tempDir, file));
          console.log(`🗑️ Deleted leftover database: ${file}`);
        } catch (error) {
          console.warn(`⚠️ Could not delete ${file}: ${error}`);
        }
      }
    }
  } catch (error) {
    console.warn(`⚠️ Could not scan temp directory for cleanup: ${error}`);
  }
  
  console.log('✅ Global test teardown completed');
}

export default globalTeardown;