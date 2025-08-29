import * as fs from 'fs/promises';
import * as path from 'path';

/**
 * Simple database utilities for SQLite test databases
 * Basic delete/recreate pattern with no complex logic
 */

/**
 * Creates a unique database path for testing
 */
export function createTestDatabasePath(): string {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(7);
  return path.join(tempDir, `CrudTest_${timestamp}_${random}.db`);
}

/**
 * Deletes a database file if it exists
 */
export async function deleteDatabase(dbPath: string): Promise<void> {
  try {
    await fs.unlink(dbPath);
  } catch (error: any) {
    // Ignore ENOENT (file doesn't exist)
    if (error.code !== 'ENOENT') {
      console.warn(`Could not delete database: ${error.message}`);
    }
  }
}

/**
 * Resets database by simply deleting it
 * The API will create a new one on startup
 */
export async function resetDatabase(dbPath: string): Promise<void> {
  await deleteDatabase(dbPath);
}

/**
 * Cleans up old test databases
 */
export async function cleanupOldTestDatabases(): Promise<void> {
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
        const oneHourInMs = 60 * 60 * 1000;
        
        if (ageInMs > oneHourInMs) {
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
 * Ensures directory exists for database
 */
export async function ensureDatabaseDirectory(dbPath: string): Promise<void> {
  const dir = path.dirname(dbPath);
  await fs.mkdir(dir, { recursive: true });
}