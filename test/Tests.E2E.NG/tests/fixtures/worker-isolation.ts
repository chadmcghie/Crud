// Playwright fixture for worker-based database isolation using Respawn
// Each worker gets a clean database state using Respawn to reset all tables

import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { DatabaseRespawn } from '../helpers/database-respawn';

// Extend the base test with worker isolation using Respawn
type WorkerIsolationFixtures = {
  isolatedApiHelpers: ApiHelpers;
  databaseRespawn: DatabaseRespawn;
};

export const test = base.extend<WorkerIsolationFixtures>({
  // Create database respawn helper for each worker
  databaseRespawn: async ({}, use, testInfo) => {
    const dbRespawn = new DatabaseRespawn(testInfo.workerIndex);
    console.log(`🔄 Worker ${testInfo.workerIndex}: Initializing database respawn`);
    await use(dbRespawn);
  },

  // Create API helpers with clean database state per worker
  isolatedApiHelpers: async ({ request, databaseRespawn }, use, testInfo) => {
    console.log(`🧹 Worker ${testInfo.workerIndex}: Resetting database for clean state`);
    
    // Reset database to clean state using Respawn
    await databaseRespawn.resetDatabase(request);
    
    // Create API helpers with worker index
    const apiHelpers = new ApiHelpers(request, testInfo.workerIndex);
    
    await use(apiHelpers);
    
    // Optional: Reset again after test for extra cleanliness
    // (Not strictly necessary since each worker resets before running)
  }
});

export { expect } from '@playwright/test';
