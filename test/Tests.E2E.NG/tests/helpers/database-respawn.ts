// Database reset using Respawn for true worker isolation
// Each worker gets a clean database state using Respawn to reset tables

import { APIRequestContext } from '@playwright/test';

export class DatabaseRespawn {
  private workerIndex: number;
  private isInitialized: boolean = false;

  constructor(workerIndex: number) {
    this.workerIndex = workerIndex;
  }

  /**
   * Reset the database to a clean state for this worker
   * Uses Respawn to truncate all tables while preserving schema
   */
  async resetDatabase(request: APIRequestContext): Promise<void> {
    console.log(`🔄 Worker ${this.workerIndex}: Resetting database state with Respawn`);
    
    try {
      // Call a special API endpoint that uses Respawn to reset the database
      const response = await request.post('/api/database/reset', {
        data: {
          workerIndex: this.workerIndex,
          preserveSchema: true
        }
      });

      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to reset database: ${response.status()} - ${errorText}`);
      }

      console.log(`✅ Worker ${this.workerIndex}: Database reset completed`);
      this.isInitialized = true;
    } catch (error) {
      console.error(`❌ Worker ${this.workerIndex}: Failed to reset database:`, error);
      throw error;
    }
  }

  /**
   * Initialize database with seed data if needed
   */
  async initializeWithSeedData(request: APIRequestContext): Promise<void> {
    if (!this.isInitialized) {
      await this.resetDatabase(request);
    }

    console.log(`🌱 Worker ${this.workerIndex}: Initializing seed data`);
    
    try {
      // Call API endpoint to seed initial data
      const response = await request.post('/api/database/seed', {
        data: {
          workerIndex: this.workerIndex
        }
      });

      if (!response.ok()) {
        console.warn(`⚠️  Worker ${this.workerIndex}: Seed data initialization warning: ${response.status()}`);
        // Don't throw - tests can run without seed data
      } else {
        console.log(`✅ Worker ${this.workerIndex}: Seed data initialized`);
      }
    } catch (error) {
      console.warn(`⚠️  Worker ${this.workerIndex}: Seed data initialization error:`, error);
      // Don't throw - tests can run without seed data
    }
  }

  /**
   * Get the worker index
   */
  getWorkerIndex(): number {
    return this.workerIndex;
  }

  /**
   * Check if database is initialized
   */
  isReady(): boolean {
    return this.isInitialized;
  }
}

