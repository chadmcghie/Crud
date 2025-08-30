import { test, expect } from '@playwright/test';
import * as fs from 'fs/promises';
import * as path from 'path';
import { createTestDatabase, resetDatabase } from './database-utils';

/**
 * Tests to verify database isolation between test files
 * Ensures each test file gets a clean database state
 */
test.describe('Database Isolation Verification', () => {
  const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';

  test('should create unique database paths for each test run', async () => {
    const db1 = createTestDatabase('IsolationTest1');
    const db2 = createTestDatabase('IsolationTest2');
    
    // Paths should be different
    expect(db1.path).not.toBe(db2.path);
    
    // Both should include timestamp and random component
    expect(db1.path).toMatch(/CrudTest_.*_\d+_\w+\.db$/);
    expect(db2.path).toMatch(/CrudTest_.*_\d+_\w+\.db$/);
  });

  test('should reset database by deleting the file', async () => {
    const testDb = createTestDatabase('ResetTest');
    
    // Create a test database file
    await fs.writeFile(testDb.path, 'test content');
    
    // Verify it exists
    let exists = await fs.access(testDb.path).then(() => true).catch(() => false);
    expect(exists).toBe(true);
    
    // Reset the database
    await resetDatabase(testDb.path);
    
    // Verify it no longer exists
    exists = await fs.access(testDb.path).then(() => true).catch(() => false);
    expect(exists).toBe(false);
    
    // Clean up if somehow still exists
    await fs.unlink(testDb.path).catch(() => {});
  });

  test('should handle concurrent database resets without conflicts', async () => {
    const databases = [];
    
    // Create multiple test databases
    for (let i = 0; i < 5; i++) {
      const db = createTestDatabase(`ConcurrentTest${i}`);
      databases.push(db);
      await fs.writeFile(db.path, `content ${i}`);
    }
    
    // Reset all databases concurrently
    const resetPromises = databases.map(db => resetDatabase(db.path));
    await Promise.all(resetPromises);
    
    // Verify all are deleted
    for (const db of databases) {
      const exists = await fs.access(db.path).then(() => true).catch(() => false);
      expect(exists).toBe(false);
    }
  });

  test('should not affect other test databases when resetting one', async () => {
    const db1 = createTestDatabase('Independent1');
    const db2 = createTestDatabase('Independent2');
    
    // Create both database files
    await fs.writeFile(db1.path, 'content 1');
    await fs.writeFile(db2.path, 'content 2');
    
    // Reset only the first database
    await resetDatabase(db1.path);
    
    // First should be deleted
    const exists1 = await fs.access(db1.path).then(() => true).catch(() => false);
    expect(exists1).toBe(false);
    
    // Second should still exist
    const exists2 = await fs.access(db2.path).then(() => true).catch(() => false);
    expect(exists2).toBe(true);
    
    // Clean up
    await fs.unlink(db2.path).catch(() => {});
  });

  test('should provide isolation through unique naming', async () => {
    const names = new Set<string>();
    
    // Generate multiple database paths
    for (let i = 0; i < 10; i++) {
      const db = createTestDatabase('UniquenessTest');
      names.add(db.path);
      
      // Small delay to ensure timestamp changes
      await new Promise(resolve => setTimeout(resolve, 10));
    }
    
    // All paths should be unique
    expect(names.size).toBe(10);
  });

  test('should clean up old databases without affecting new ones', async () => {
    // Create an old database (mock by creating and immediately treating as old)
    const oldDb = path.join(tempDir, 'CrudTest_Old_12345_abc.db');
    await fs.writeFile(oldDb, 'old content');
    
    // Create a new database
    const newDb = createTestDatabase('NewTest');
    await fs.writeFile(newDb.path, 'new content');
    
    // Manually set old database's modified time to 2 hours ago
    const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000);
    await fs.utimes(oldDb, twoHoursAgo, twoHoursAgo);
    
    // Run cleanup (from database-utils)
    const { cleanupTestDatabases } = await import('./database-utils');
    await cleanupTestDatabases();
    
    // Old database should be deleted
    const oldExists = await fs.access(oldDb).then(() => true).catch(() => false);
    expect(oldExists).toBe(false);
    
    // New database should still exist
    const newExists = await fs.access(newDb.path).then(() => true).catch(() => false);
    expect(newExists).toBe(true);
    
    // Clean up
    await fs.unlink(newDb.path).catch(() => {});
  });
});