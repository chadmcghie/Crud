import { FullConfig } from '@playwright/test';
import fs from 'fs/promises';
import path from 'path';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global test teardown...');
  
  const workers = parseInt(process.env.TOTAL_WORKERS || '1', 10);
  
  // Clean up worker-specific database files
  for (let i = 0; i < workers; i++) {
    const workerDatabase = process.env[`WORKER_${i}_DATABASE`];
    
    if (workerDatabase) {
      try {
        await fs.access(workerDatabase);
        await fs.unlink(workerDatabase);
        console.log(`🗑️ Cleaned up database for worker ${i}: ${path.basename(workerDatabase)}`);
      } catch (error) {
        // File doesn't exist or already cleaned up
        console.log(`ℹ️ Database for worker ${i} already cleaned up or doesn't exist`);
      }
    }
  }
  
  // Clean up any remaining test database files in temp directory
  try {
    const tempDir = '/tmp';
    const files = await fs.readdir(tempDir);
    const testDbFiles = files.filter(file => file.startsWith('CrudTest_Worker') && file.endsWith('.db'));
    
    for (const file of testDbFiles) {
      try {
        await fs.unlink(path.join(tempDir, file));
        console.log(`🗑️ Cleaned up orphaned database file: ${file}`);
      } catch (error) {
        console.warn(`⚠️ Could not clean up ${file}: ${error}`);
      }
    }
  } catch (error) {
    console.warn(`⚠️ Could not scan temp directory for cleanup: ${error}`);
  }
  
  console.log('✅ Global test teardown completed');
}

export default globalTeardown;