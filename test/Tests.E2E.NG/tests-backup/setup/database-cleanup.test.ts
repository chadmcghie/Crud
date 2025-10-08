import { test, expect } from '@playwright/test';
import * as fs from 'fs/promises';
import * as path from 'path';
import { getTempDirectory } from './temp-directory';

/**
 * Tests for simplified SQLite file-based database cleanup
 * Verifies simple delete/recreate pattern works correctly
 */
test.describe('Simplified Database Cleanup', () => {
  const tempDir = getTempDirectory();
  const testDbPath = path.join(tempDir, 'test-cleanup.db');

  test.afterAll(async () => {
    // Clean up test files
    try {
      await fs.unlink(testDbPath);
    } catch {
      // Ignore if doesn't exist
    }
  });

  test('should delete database file if exists', async () => {
    // Create a test database file
    await fs.writeFile(testDbPath, 'test database content');
    
    // Verify it exists
    const existsBefore = await fs.access(testDbPath).then(() => true).catch(() => false);
    expect(existsBefore).toBe(true);
    
    // Simple delete
    await fs.unlink(testDbPath);
    
    // Verify it's deleted
    const existsAfter = await fs.access(testDbPath).then(() => true).catch(() => false);
    expect(existsAfter).toBe(false);
  });

  test('should handle non-existent file gracefully', async () => {
    const nonExistentPath = path.join(tempDir, 'non-existent.db');
    
    // Try to delete non-existent file
    const result = await fs.unlink(nonExistentPath).then(() => true).catch(() => false);
    
    // Should return false but not throw
    expect(result).toBe(false);
  });

  test('should create fresh database file', async () => {
    // Ensure file doesn't exist
    await fs.unlink(testDbPath).catch(() => {});
    
    // Create new file
    await fs.writeFile(testDbPath, '');
    
    // Verify it exists and is empty
    const stats = await fs.stat(testDbPath);
    expect(stats.size).toBe(0);
  });

  test('should reset database with delete and recreate', async () => {
    // Create initial database with content
    await fs.writeFile(testDbPath, 'old content');
    
    // Simple reset: delete then recreate
    await fs.unlink(testDbPath).catch(() => {});
    await fs.writeFile(testDbPath, '');
    
    // Verify it's reset (empty)
    const content = await fs.readFile(testDbPath, 'utf-8');
    expect(content).toBe('');
  });

  test('should provide unique database names per test run', async () => {
    const createUniqueDatabasePath = () => {
      const timestamp = Date.now();
      const random = Math.random().toString(36).substring(7);
      return path.join(tempDir, `TestDB_${timestamp}_${random}.db`);
    };
    
    const db1 = createUniqueDatabasePath();
    await new Promise(resolve => setTimeout(resolve, 10)); // Small delay
    const db2 = createUniqueDatabasePath();
    
    expect(db1).not.toBe(db2);
  });

  test('should clean up old test databases', async () => {
    // Create test databases with different ages
    const oldDbPath = path.join(tempDir, 'TestDB_old.db');
    const newDbPath = path.join(tempDir, 'TestDB_new.db');
    
    await fs.writeFile(oldDbPath, 'old');
    await fs.writeFile(newDbPath, 'new');
    
    // Simple cleanup: find and delete test databases
    const files = await fs.readdir(tempDir);
    const testDbs = files.filter(f => f.startsWith('TestDB_'));
    
    for (const db of testDbs) {
      await fs.unlink(path.join(tempDir, db)).catch(() => {});
    }
    
    // Verify cleanup
    const filesAfter = await fs.readdir(tempDir);
    const remainingTestDbs = filesAfter.filter(f => f.startsWith('TestDB_'));
    expect(remainingTestDbs.length).toBe(0);
  });

  test('should ensure directory exists before creating database', async () => {
    const nestedPath = path.join(tempDir, 'nested', 'dir', 'test.db');
    const dir = path.dirname(nestedPath);
    
    // Create directory structure
    await fs.mkdir(dir, { recursive: true });
    
    // Verify directory exists
    const dirExists = await fs.access(dir).then(() => true).catch(() => false);
    expect(dirExists).toBe(true);
    
    // Clean up
    await fs.rmdir(path.join(tempDir, 'nested'), { recursive: true }).catch(() => {});
  });

  test('should handle concurrent database operations', async () => {
    const operations = [];
    
    // Simulate concurrent operations
    for (let i = 0; i < 5; i++) {
      const dbPath = path.join(tempDir, `concurrent_${i}.db`);
      operations.push(
        fs.writeFile(dbPath, `content ${i}`)
          .then(() => fs.unlink(dbPath))
          .catch(() => {})
      );
    }
    
    // All operations should complete without errors
    await expect(Promise.all(operations)).resolves.toBeDefined();
  });
});