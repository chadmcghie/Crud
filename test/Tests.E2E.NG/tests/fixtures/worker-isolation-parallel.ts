// Enhanced Playwright fixture for true worker isolation using per-worker API servers
// Each worker gets its own API server instance with its own database for complete isolation

import { test as base, APIRequestContext } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { DatabaseRespawn } from '../helpers/database-respawn';
import { spawn, ChildProcess } from 'child_process';
import path from 'path';
import { promisify } from 'util';

const sleep = promisify(setTimeout);

type WorkerIsolationFixtures = {
  isolatedApiHelpers: ApiHelpers;
  databaseRespawn: DatabaseRespawn;
  workerApiServer: string;
};

export const test = base.extend<WorkerIsolationFixtures>({
  // Start a dedicated API server for each worker with its own database
  workerApiServer: [async ({}, use, testInfo) => {
    const workerPort = 5172 + testInfo.workerIndex; // Each worker gets its own port
    const timestamp = Date.now();
    const workerDbFile = `CrudAppTest_Worker${testInfo.workerIndex}_${timestamp}.db`;
    
    console.log(`🚀 Worker ${testInfo.workerIndex}: Starting dedicated API server on port ${workerPort} with database ${workerDbFile}`);
    
    let apiProcess: ChildProcess | null = null;
    
    try {
      // Start the API with worker-specific configuration
      apiProcess = spawn('dotnet', ['run'], {
        cwd: path.join(__dirname, '../../../../src/Api'),
        env: {
          ...process.env,
          ASPNETCORE_URLS: `http://localhost:${workerPort}`,
          ConnectionStrings__DefaultConnection: `Data Source=${workerDbFile}`,
          ASPNETCORE_ENVIRONMENT: 'Testing',
          WORKER_INDEX: testInfo.workerIndex.toString(),
          WORKER_DATABASE: workerDbFile
        },
        stdio: ['pipe', 'pipe', 'pipe']
      });

      // Wait for server to start with timeout
      const serverUrl = `http://localhost:${workerPort}`;
      let serverStarted = false;
      let attempts = 0;
      const maxAttempts = 60; // 30 seconds with 500ms intervals

      while (!serverStarted && attempts < maxAttempts) {
        try {
          // Try to make a health check request
          const response = await fetch(`${serverUrl}/health`);
          if (response.ok) {
            serverStarted = true;
            console.log(`✅ Worker ${testInfo.workerIndex}: API server started successfully on ${serverUrl}`);
          }
        } catch (error) {
          // Server not ready yet, wait and retry
          await sleep(500);
          attempts++;
        }
      }

      if (!serverStarted) {
        throw new Error(`Worker ${testInfo.workerIndex}: API server failed to start within timeout`);
      }

      await use(serverUrl);
      
    } finally {
      // Cleanup: Kill the process and delete the database file
      if (apiProcess) {
        apiProcess.kill('SIGTERM');
        
        // Wait a bit for graceful shutdown
        await sleep(1000);
        
        if (!apiProcess.killed) {
          apiProcess.kill('SIGKILL');
        }
        
        console.log(`🛑 Worker ${testInfo.workerIndex}: Stopped API server`);
      }
      
      // Clean up database file
      try {
        const fs = await import('fs');
        if (fs.existsSync(workerDbFile)) {
          fs.unlinkSync(workerDbFile);
          console.log(`🗑️ Worker ${testInfo.workerIndex}: Cleaned up database file ${workerDbFile}`);
        }
      } catch (error) {
        console.warn(`⚠️ Worker ${testInfo.workerIndex}: Failed to cleanup database file ${workerDbFile}:`, error);
      }
    }
  }, { scope: 'worker' }],

  // Create database respawn helper for each worker
  databaseRespawn: async ({ workerApiServer }, use, testInfo) => {
    const dbRespawn = new DatabaseRespawn(testInfo.workerIndex);
    console.log(`🔄 Worker ${testInfo.workerIndex}: Initializing database respawn for server ${workerApiServer}`);
    await use(dbRespawn);
  },

  // Create API helpers with worker-specific server and clean database state
  isolatedApiHelpers: async ({ request, databaseRespawn, workerApiServer }, use, testInfo) => {
    console.log(`🧹 Worker ${testInfo.workerIndex}: Setting up isolated API helpers for ${workerApiServer}`);
    
    // Create a new request context pointing to the worker-specific server
    const workerRequest = await request.newContext({
      baseURL: workerApiServer,
      timeout: 30000 // Increase timeout for worker-specific servers
    });
    
    // Reset database to clean state using Respawn
    await databaseRespawn.resetDatabase(workerRequest);
    
    // Create API helpers with worker-specific request context
    const apiHelpers = new ApiHelpers(workerRequest, testInfo.workerIndex);
    
    await use(apiHelpers);
    
    // Cleanup request context
    await workerRequest.dispose();
  }
});

export { expect } from '@playwright/test';
