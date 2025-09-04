import * as fs from 'fs/promises';
import * as path from 'path';

/**
 * Simple database utilities for SQLite test databases
 * Basic delete/recreate pattern for test isolation
 */

/**
 * Creates a unique test database path
 */
export function createTestDatabase(name: string = 'Test'): { path: string } {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(7);
  const dbPath = path.join(tempDir, `CrudTest_${name}_${timestamp}_${random}.db`);
  
  return { path: dbPath };
}

/**
 * Deletes a database file if it exists
 */
export async function deleteDatabase(dbPath: string): Promise<void> {
  try {
    await fs.unlink(dbPath);
  } catch (error: any) {
    // Ignore if file doesn't exist
    if (error.code !== 'ENOENT') {
      console.warn(`Could not delete database: ${error.message}`);
    }
  }
}

/**
 * Resets database by deleting it
 * The API will create a new one on next access
 */
export async function resetDatabase(dbPath: string): Promise<void> {
  await deleteDatabase(dbPath);
}

/**
 * Cleans up old test databases
 */
export async function cleanupTestDatabases(): Promise<void> {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  
  try {
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('CrudTest_') && f.endsWith('.db'));
    
    for (const db of testDbs) {
      try {
        const filePath = path.join(tempDir, db);
        const stats = await fs.stat(filePath);
        
        // Delete databases older than 1 hour
        const ageInMs = Date.now() - stats.mtime.getTime();
        if (ageInMs > 60 * 60 * 1000) {
          await deleteDatabase(filePath);
        }
      } catch {
        // Ignore individual file errors
      }
    }
  } catch {
    // Ignore directory scan errors
  }
}

/**
 * Gets database file size (for debugging only)
 */
export async function getDatabaseSize(dbPath: string): Promise<number> {
  try {
    const stats = await fs.stat(dbPath);
    return stats.size;
  } catch {
    return 0;
  }
}