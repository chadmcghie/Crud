import * as path from 'path';
import * as fs from 'fs/promises';

/**
 * Global teardown for webServer configuration
 * Cleans up test database files created during the test run
 */
async function globalTeardown() {
  // Get the database path from the test run
  const testRunId = process.env.CI ? process.env.TEST_RUN_ID : 'local';
  if (!testRunId || testRunId === 'local') {
    // For local runs, we keep the database for debugging
    console.log('Local test run - keeping database for debugging');
    return;
  }
  
  const databasePath = path.join(process.cwd(), '..', '..', `CrudTest_${testRunId}.db`);
  
  // Clean up all SQLite files (main db, WAL, and shared memory)
  const filesToDelete = [
    databasePath,
    `${databasePath}-wal`,
    `${databasePath}-shm`
  ];
  
  console.log('Cleaning up test database files...');
  
  for (const file of filesToDelete) {
    try {
      await fs.unlink(file);
      console.log(`✅ Deleted: ${path.basename(file)}`);
    } catch (error: any) {
      if (error?.code !== 'ENOENT') {
        console.warn(`⚠️ Could not delete ${path.basename(file)}:`, error?.message);
      }
    }
  }
  
  // Also clean up any old test databases (older than 1 hour)
  if (process.env.CI) {
    try {
      const dir = path.dirname(databasePath);
      const files = await fs.readdir(dir);
      const now = Date.now();
      const oneHour = 60 * 60 * 1000;
      
      for (const file of files) {
        if (file.startsWith('CrudTest_') && file.endsWith('.db')) {
          const filePath = path.join(dir, file);
          try {
            const stats = await fs.stat(filePath);
            if (now - stats.mtimeMs > oneHour) {
              await fs.unlink(filePath);
              // Also try to delete associated WAL and SHM files
              await fs.unlink(`${filePath}-wal`).catch(() => {});
              await fs.unlink(`${filePath}-shm`).catch(() => {});
              console.log(`🧹 Cleaned up old database: ${file}`);
            }
          } catch {
            // Ignore errors for individual files
          }
        }
      }
    } catch {
      // Ignore errors during cleanup
    }
  }
}

export default globalTeardown;