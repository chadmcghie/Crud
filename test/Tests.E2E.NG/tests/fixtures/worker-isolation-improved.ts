// Improved Playwright fixture for worker-based database isolation
// Uses shared API server with per-worker database reset for better performance

import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { DatabaseRespawn } from '../helpers/database-respawn';

type WorkerIsolationFixtures = {
  isolatedApiHelpers: ApiHelpers;
  databaseRespawn: DatabaseRespawn;
  workerIndex: number;
};

export const test = base.extend<WorkerIsolationFixtures>({
  // Get the worker index for this test
  workerIndex: async ({}, use, testInfo) => {
    await use(testInfo.workerIndex);
  },

  // Create database respawn helper for each worker
  databaseRespawn: async ({ workerIndex }, use, testInfo) => {
    const dbRespawn = new DatabaseRespawn(workerIndex);
    console.log(`🔄 Worker ${workerIndex}: Initializing database respawn`);
    await use(dbRespawn);
  },

  // Create API helpers with worker-specific database isolation
  isolatedApiHelpers: async ({ request, databaseRespawn, workerIndex }, use, testInfo) => {
    console.log(`🧹 Worker ${workerIndex}: Setting up isolated database state`);
    
    // Add a small delay between workers to avoid database conflicts
    if (workerIndex > 0) {
      await new Promise(resolve => setTimeout(resolve, workerIndex * 100));
    }
    
    try {
      // Reset database to clean state using Respawn
      await databaseRespawn.resetDatabase(request);
      console.log(`✅ Worker ${workerIndex}: Database reset completed`);
      
      // Create API helpers with worker index
      const apiHelpers = new ApiHelpers(request, workerIndex);
      
      await use(apiHelpers);
      
    } catch (error) {
      console.warn(`⚠️ Worker ${workerIndex}: Database reset failed, continuing with cleanup-based isolation:`, error);
      
      // Fallback to the existing cleanup-based approach
      const apiHelpers = new ApiHelpers(request, workerIndex);
      await apiHelpers.cleanupAll();
      
      await use(apiHelpers);
    }
  }
});

export { expect } from '@playwright/test';
