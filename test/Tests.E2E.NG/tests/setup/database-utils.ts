import * as fs from 'fs/promises';
import * as path from 'path';
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

/**
 * Database utilities for managing SQLite test databases
 * Handles proper cleanup and isolation for serial test execution
 */

export interface DatabaseConfig {
  path: string;
  connectionString: string;
}

/**
 * Creates a new test database configuration
 */
export function createTestDatabase(name: string = 'Serial'): DatabaseConfig {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  const timestamp = Date.now();
  const dbPath = path.join(tempDir, `CrudTest_${name}_${timestamp}.db`);
  
  return {
    path: dbPath,
    connectionString: `Data Source=${dbPath}`
  };
}

/**
 * Deletes a SQLite database file with retry logic
 */
export async function deleteDatabase(dbPath: string, maxRetries: number = 5): Promise<boolean> {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      // Check if file exists
      await fs.access(dbPath);
      
      // Try to delete the file
      await fs.unlink(dbPath);
      console.log(`✅ Deleted database: ${path.basename(dbPath)}`);
      return true;
    } catch (error: any) {
      if (error.code === 'ENOENT') {
        // File doesn't exist, that's fine
        return true;
      }
      
      if (error.code === 'EBUSY' || error.code === 'EPERM') {
        // File is locked, wait and retry
        if (attempt < maxRetries) {
          console.log(`⏳ Database locked, retrying (${attempt}/${maxRetries})...`);
          await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
          continue;
        }
      }
      
      console.warn(`⚠️ Failed to delete database after ${maxRetries} attempts:`, error.message);
      return false;
    }
  }
  
  return false;
}

/**
 * Cleans up all test databases in the temp directory
 */
export async function cleanupTestDatabases(olderThanHours: number = 1): Promise<void> {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
  
  try {
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('CrudTest_') && f.endsWith('.db'));
    
    for (const db of testDbs) {
      try {
        const filePath = path.join(tempDir, db);
        const stats = await fs.stat(filePath);
        const ageInHours = (Date.now() - stats.mtime.getTime()) / (1000 * 60 * 60);
        
        if (ageInHours > olderThanHours) {
          const deleted = await deleteDatabase(filePath, 3);
          if (deleted) {
            console.log(`🗑️ Cleaned up old database: ${db} (${ageInHours.toFixed(1)} hours old)`);
          }
        }
      } catch (err) {
        // Ignore individual file errors
      }
    }
  } catch (err) {
    console.warn('⚠️ Could not scan for test databases:', err);
  }
}

/**
 * Resets database by deleting and creating a fresh one
 */
export async function resetDatabase(dbPath: string): Promise<void> {
  // Delete existing database
  await deleteDatabase(dbPath, 3);
  
  // Ensure directory exists
  const dir = path.dirname(dbPath);
  await fs.mkdir(dir, { recursive: true });
  
  console.log(`🔄 Database reset: ${path.basename(dbPath)}`);
}

/**
 * Force closes all connections to a SQLite database (Windows)
 */
export async function forceCloseDatabase(dbPath: string): Promise<void> {
  if (process.platform === 'win32') {
    try {
      // Use Windows handle tool to force close file handles
      const fileName = path.basename(dbPath);
      await execAsync(`handle.exe -accepteula -c ${dbPath} -y`, { 
        timeout: 5000 
      }).catch(() => {
        // Handle.exe might not be available, that's okay
      });
    } catch (err) {
      // Ignore errors, this is best-effort
    }
  }
  
  // Give time for handles to be released
  await new Promise(resolve => setTimeout(resolve, 500));
}

/**
 * Gets database file size
 */
export async function getDatabaseSize(dbPath: string): Promise<number> {
  try {
    const stats = await fs.stat(dbPath);
    return stats.size;
  } catch {
    return 0;
  }
}

/**
 * Validates database is accessible
 */
export async function isDatabaseAccessible(dbPath: string): Promise<boolean> {
  try {
    await fs.access(dbPath, fs.constants.R_OK | fs.constants.W_OK);
    return true;
  } catch {
    return false;
  }
}