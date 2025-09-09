// API helper functions for E2E tests with Polly-inspired resilience patterns
import { APIRequestContext } from '@playwright/test';
import { TestRole, TestPerson, TestWall } from './test-data';

interface RetryOptions {
  maxRetries: number;
  baseDelayMs: number;
  maxDelayMs: number;
  useJitter: boolean;
  shouldRetry?: (error: any) => boolean;
}

interface CircuitBreakerOptions {
  failureThreshold: number;
  resetTimeoutMs: number;
  monitoringPeriodMs: number;
}

enum CircuitState {
  Closed,
  Open,
  HalfOpen
}

class CircuitBreaker {
  private state = CircuitState.Closed;
  private failureCount = 0;
  private lastFailureTime = 0;
  private successCount = 0;

  constructor(private options: CircuitBreakerOptions) {}

  async execute<T>(operation: () => Promise<T>): Promise<T> {
    if (this.state === CircuitState.Open) {
      if (Date.now() - this.lastFailureTime > this.options.resetTimeoutMs) {
        this.state = CircuitState.HalfOpen;
        this.successCount = 0;
      } else {
        throw new Error('Circuit breaker is OPEN - operation not executed');
      }
    }

    try {
      const result = await operation();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  private onSuccess(): void {
    this.failureCount = 0;
    if (this.state === CircuitState.HalfOpen) {
      this.successCount++;
      if (this.successCount >= 2) { // Require 2 successes to close
        this.state = CircuitState.Closed;
      }
    }
  }

  private onFailure(): void {
    this.failureCount++;
    this.lastFailureTime = Date.now();
    
    if (this.failureCount >= this.options.failureThreshold) {
      this.state = CircuitState.Open;
    }
  }
}

export class ApiHelpers {
  private circuitBreaker: CircuitBreaker;
  private workerId: string;
  private testId: string;

  constructor(private request: APIRequestContext, workerIndex?: number) {
    this.circuitBreaker = new CircuitBreaker({
      failureThreshold: 3,
      resetTimeoutMs: 10000, // 10 seconds
      monitoringPeriodMs: 30000 // 30 seconds
    });
    
    // Use worker ID for better test isolation - accept it as parameter from test info
    this.workerId = workerIndex !== undefined ? workerIndex.toString() : '0';
    // Add test-scoped ID to prevent cross-test interference - use high precision timestamp
    this.testId = `T${Date.now()}_${process.hrtime.bigint().toString(36).substr(-6)}`;
  }



  // Enhanced retry logic with exponential backoff and jitter
  private async retryOperation<T>(
    operation: () => Promise<T>,
    operationName: string,
    options: Partial<RetryOptions> = {}
  ): Promise<T> {
    const retryOptions: RetryOptions = {
      maxRetries: 3, // Reduced - let Playwright handle retries when possible
      baseDelayMs: 100, // Shorter delays for faster tests
      maxDelayMs: 1000, // Reduced max delay
      useJitter: false, // Disable jitter for more deterministic tests
      shouldRetry: (error) => {
        // Retry on network errors, timeouts, 5xx errors, and race condition indicators
        const errorMessage = error.message?.toLowerCase() || '';
        return errorMessage.includes('timeout') || 
               errorMessage.includes('econnreset') ||
               errorMessage.includes('500') ||
               errorMessage.includes('502') ||
               errorMessage.includes('503') ||
               errorMessage.includes('504') ||
               errorMessage.includes('not found') || // Race condition: resource deleted
               errorMessage.includes('400'); // May indicate temporary data inconsistency
      },
      ...options
    };

    return await this.circuitBreaker.execute(async () => {
      let lastError: Error;
      
      for (let attempt = 0; attempt <= retryOptions.maxRetries; attempt++) {
        try {
          const result = await operation();
          if (attempt > 0) {
            console.log(`✅ ${operationName} succeeded on attempt ${attempt + 1}`);
          }
          return result;
        } catch (error) {
          lastError = error as Error;
          
          if (attempt === retryOptions.maxRetries || !retryOptions.shouldRetry!(error)) {
            console.error(`❌ ${operationName} failed after ${attempt + 1} attempts:`, error);
            throw error;
          }

          // Use a minimal delay instead of exponential backoff for faster, more deterministic tests
          const delay = Math.min(retryOptions.baseDelayMs * (attempt + 1), retryOptions.maxDelayMs);
          console.warn(`⚠️  ${operationName} failed (attempt ${attempt + 1}/${retryOptions.maxRetries + 1}), retrying in ${delay}ms:`, error);
          await this.sleep(delay);
        }
      }
      
      throw lastError!;
    });
  }

  private calculateDelay(attempt: number, options: RetryOptions): number {
    // Exponential backoff with jitter
    let delay = options.baseDelayMs * Math.pow(2, attempt);
    delay = Math.min(delay, options.maxDelayMs);
    
    if (options.useJitter) {
      // Add random jitter ±25%
      const jitter = delay * 0.25 * (Math.random() * 2 - 1);
      delay += jitter;
    }
    
    return Math.max(delay, 50); // Minimum 50ms delay
  }

  private async sleep(ms: number): Promise<void> {
    // Use Playwright's page.waitForTimeout() instead of setTimeout for better test reliability
    // This is more deterministic and works better with Playwright's timing
    return new Promise(resolve => process.nextTick(() => {
      setTimeout(resolve, ms);
    }));
  }

  // Generate worker-specific test data to avoid conflicts
  private generateUniqueId(): string {
    return `W${this.workerId}_${this.testId}_${Math.random().toString(36).substr(2, 6)}`;
  }

  // Role API helpers with resilience
  async createRole(role: TestRole): Promise<any> {
    // If role name already has worker prefix or is an explicit test name, use as-is
    const hasWorkerPrefix = role.name.match(/^W\d+_/);
    const isExplicitTestName = role.name.includes('ParallelTest_') || role.name.includes('ConcurrentRole_') || 
                               role.name.includes('CleanStateTest_') || role.name.includes('Worker') ||
                               role.name.includes('SerialTest_') || role.name.includes('Serial_') ||
                               role.name === 'API Role' || role.name === 'UI Role';
    const hasExplicitDescription = role.description && (role.description.includes('Test role for') || role.description.includes('Worker'));
    
    const roleData = {
      ...role,
      name: (hasWorkerPrefix || isExplicitTestName) ? role.name : `${this.generateUniqueId()}_Role_${role.name}`,
      description: role.description ? ((hasWorkerPrefix || isExplicitTestName || hasExplicitDescription) ? role.description : `Worker${this.workerId}: ${role.description}`) : undefined
    };

    return this.retryOperation(async () => {
      const response = await this.request.post('/api/roles', {
        data: roleData
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create role: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'createRole');
  }

  async getRoles(): Promise<any[]> {
    return this.retryOperation(async () => {
      const response = await this.request.get('/api/roles');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get roles: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'getRoles');
  }

  async getRole(id: string): Promise<any> {
    return this.retryOperation(async () => {
      const response = await this.request.get(`/api/roles/${id}`);
      if (!response.ok()) {
        throw new Error(`Failed to get role: ${response.status()}`);
      }
      return await response.json();
    }, 'getRole');
  }

  async updateRole(id: string, role: TestRole): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.put(`/api/roles/${id}`, {
        data: role
      });
      if (!response.ok()) {
        throw new Error(`Failed to update role: ${response.status()}`);
      }
    }, 'updateRole');
  }

  async deleteRole(id: string): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.delete(`/api/roles/${id}`);
      if (!response.ok() && response.status() !== 404) {
        throw new Error(`Failed to delete role: ${response.status()}`);
      }
      // 404 is OK - role already deleted (idempotent)
    }, 'deleteRole', {
      shouldRetry: (error) => {
        // Don't retry 404s (already deleted)
        return !error.message?.includes('404');
      }
    });
  }

  // Person API helpers with resilience
  async createPerson(person: TestPerson): Promise<any> {
    // Use the name as-is since generateTestPerson now creates valid names
    const personData = {
      ...person,
      phone: person.phone || `+1-555-${Math.floor(Math.random() * 10000).toString().padStart(4, '0')}`
    };

    return this.retryOperation(async () => {
      const response = await this.request.post('/api/people', {
        data: personData
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create person: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'createPerson');
  }

  async getPeople(): Promise<any[]> {
    return this.retryOperation(async () => {
      const response = await this.request.get('/api/people');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get people: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'getPeople');
  }

  async getPerson(id: string): Promise<any> {
    return this.retryOperation(async () => {
      const response = await this.request.get(`/api/people/${id}`);
      if (!response.ok()) {
        throw new Error(`Failed to get person: ${response.status()}`);
      }
      return await response.json();
    }, 'getPerson');
  }

  async updatePerson(id: string, person: TestPerson): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.put(`/api/people/${id}`, {
        data: person
      });
      if (!response.ok()) {
        throw new Error(`Failed to update person: ${response.status()}`);
      }
    }, 'updatePerson');
  }

  async deletePerson(id: string): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.delete(`/api/people/${id}`);
      if (!response.ok() && response.status() !== 404) {
        throw new Error(`Failed to delete person: ${response.status()}`);
      }
      // 404 is OK - person already deleted (idempotent)
    }, 'deletePerson', {
      shouldRetry: (error) => {
        return !error.message?.includes('404');
      }
    });
  }

  // Wall API helpers with resilience
  async createWall(wall: TestWall): Promise<any> {
    // Add worker-specific prefix to avoid naming conflicts
    const uniqueWall = {
      ...wall,
      name: `${this.generateUniqueId()}_Wall_${wall.name}`,
      description: wall.description ? `Worker${this.workerId}: ${wall.description}` : undefined
    };

    return this.retryOperation(async () => {
      const response = await this.request.post('/api/walls', {
        data: uniqueWall
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create wall: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'createWall');
  }

  async getWalls(): Promise<any[]> {
    return this.retryOperation(async () => {
      const response = await this.request.get('/api/walls');
      if (!response.ok()) {
        throw new Error(`Failed to get walls: ${response.status()}`);
      }
      return await response.json();
    }, 'getWalls');
  }

  async getWall(id: string): Promise<any> {
    return this.retryOperation(async () => {
      const response = await this.request.get(`/api/walls/${id}`);
      if (!response.ok()) {
        throw new Error(`Failed to get wall: ${response.status()}`);
      }
      return await response.json();
    }, 'getWall');
  }

  async updateWall(id: string, wall: TestWall): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.put(`/api/walls/${id}`, {
        data: wall
      });
      if (!response.ok()) {
        throw new Error(`Failed to update wall: ${response.status()}`);
      }
    }, 'updateWall');
  }

  async deleteWall(id: string): Promise<void> {
    return this.retryOperation(async () => {
      const response = await this.request.delete(`/api/walls/${id}`);
      if (!response.ok() && response.status() !== 404) {
        throw new Error(`Failed to delete wall: ${response.status()}`);
      }
      // 404 is OK - wall already deleted (idempotent)
    }, 'deleteWall', {
      shouldRetry: (error) => {
        return !error.message?.includes('404');
      }
    });
  }

  // Pre-test cleanup - only removes stale data from previous test runs
  async cleanupRoles(forceImmediate: boolean = false): Promise<void> {
    console.log(`🧹 Worker ${this.workerId}: Starting role cleanup...`);
    
    try {
      const roles = await this.getRoles();
      // Only cleanup OLD test roles (older than 1 second) for sequential execution
      // Unless forceImmediate is true, then cleanup all test roles
      const oneSecondAgo = new Date(Date.now() - 1 * 1000);
      const rolesToCleanup = roles.filter(role => {
        // Skip seed data roles (but not test roles like "UI Role")
        if (['Administrator', 'Manager', 'Developer', 'Analyst', 'User'].includes(role.name)) {
          return false;
        }
        
        // Force immediate cleanup for integration tests
        if (forceImmediate) {
          // Clean up any test-related roles (including "UI Role" and generated test roles)
          return role.name === 'UI Role' || 
                 (role.name.includes('W') && role.name.includes('_')) ||
                 role.name.startsWith('Test Role') ||
                 role.name.includes('Unique Role') ||
                 role.name.includes('Rapid Role') ||
                 role.name.includes('API Role');
        }
        
        // Only cleanup test roles that are old enough (safe for both sequential and parallel)
        if ((role.name.includes('W') && role.name.includes('_')) || 
            role.name.startsWith('Test Role') || 
            role.name.includes('Unique Role') ||
            role.name.includes('Rapid Role') ||
            role.name.includes('API Role')) {
          
          // Handle both timestamp formats:
          // New format: W{worker}_T{timestamp}_... 
          // Old format: W{worker}_{6-digit-timestamp}_...
          let timestampMatch = role.name.match(/W\d+_T(\d+)_/);
          if (timestampMatch) {
            const roleTimestamp = new Date(parseInt(timestampMatch[1]));
            return roleTimestamp < oneSecondAgo;
          }
          
          // Try old format: W{worker}_{6-digit-timestamp}_
          timestampMatch = role.name.match(/W\d+_(\d{6})_/);
          if (timestampMatch) {
            const shortTimestamp = parseInt(timestampMatch[1]);
            const currentShortTimestamp = parseInt(Date.now().toString().slice(-6));
            // If the 6-digit timestamp is more than 1000 units old (roughly 1 second)
            return (currentShortTimestamp - shortTimestamp) > 1000;
          }
        }
        
        return false;
      });
      
      if (rolesToCleanup.length === 0) {
        console.log(`✅ Worker ${this.workerId}: No stale roles to cleanup`);
        return;
      }

      console.log(`🧹 Worker ${this.workerId}: Cleaning up ${rolesToCleanup.length} stale roles (>1sec old)`);
      
      // Delete roles sequentially to avoid race conditions
      for (const role of rolesToCleanup) {
        try {
          await this.deleteRole(role.id);
          console.log(`✅ Worker ${this.workerId}: Deleted stale role ${role.id}`);
        } catch (error) {
          // Ignore 404 errors (role already deleted by another worker)
          if (!error.message?.includes('404')) {
            console.warn(`⚠️  Worker ${this.workerId}: Failed to cleanup role ${role.id}:`, error);
          }
        }
      }
      
      console.log(`✅ Worker ${this.workerId}: Role cleanup completed`);
    } catch (error) {
      console.warn(`⚠️  Worker ${this.workerId}: Role cleanup had issues but continuing:`, error);
      // Don't throw - allow tests to continue
    }
  }

  async cleanupPeople(forceImmediate: boolean = false): Promise<void> {
    console.log(`🧹 Worker ${this.workerId}: Starting people cleanup...`);
    
    try {
      const people = await this.getPeople();
      // Only cleanup OLD test people (older than 1 second) for sequential execution
      // Unless forceImmediate is true, then cleanup all test people
      const oneSecondAgo = new Date(Date.now() - 1 * 1000);
      const peopleToCleanup = people.filter(person => {
        // Force immediate cleanup for integration tests
        if (forceImmediate) {
          // Clean up any test-related people (including "UI Person" and generated test people)
          return person.fullName === 'UI Person' || 
                 (person.fullName.includes('W') && person.fullName.includes('_')) ||
                 person.fullName.includes('Rapid Person') ||
                 person.fullName.includes('API Person');
        }
        
        // Only cleanup test people that are old enough
        if ((person.fullName.includes('W') && person.fullName.includes('_')) ||
            person.fullName.includes('Rapid Person') ||
            person.fullName.includes('API Person')) {
          // Handle both timestamp formats:
          // New format: W{worker}_T{timestamp}_... 
          // Old format: W{worker}_{6-digit-timestamp}_...
          let timestampMatch = person.fullName.match(/W\d+_T(\d+)_/);
          if (timestampMatch) {
            const personTimestamp = new Date(parseInt(timestampMatch[1]));
            return personTimestamp < oneSecondAgo;
          }
          
          // Try old format: W{worker}_{6-digit-timestamp}_
          timestampMatch = person.fullName.match(/W\d+_(\d{6})_/);
          if (timestampMatch) {
            const shortTimestamp = parseInt(timestampMatch[1]);
            const currentShortTimestamp = parseInt(Date.now().toString().slice(-6));
            // If the 6-digit timestamp is more than 1000 units old (roughly 1 second)
            return (currentShortTimestamp - shortTimestamp) > 1000;
          }
        }
        
        return false;
      });
      
      if (peopleToCleanup.length === 0) {
        console.log(`✅ Worker ${this.workerId}: No stale people to cleanup`);
        return;
      }

      console.log(`🧹 Worker ${this.workerId}: Cleaning up ${peopleToCleanup.length} stale people (>1sec old)`);
      
      // Delete people sequentially to avoid race conditions
      for (const person of peopleToCleanup) {
        try {
          await this.deletePerson(person.id);
          console.log(`✅ Worker ${this.workerId}: Deleted stale person ${person.id}`);
        } catch (error) {
          // Ignore 404 errors (person already deleted by another worker)
          if (!error.message?.includes('404')) {
            console.warn(`⚠️  Worker ${this.workerId}: Failed to cleanup person ${person.id}:`, error);
          }
        }
      }
      
      console.log(`✅ Worker ${this.workerId}: People cleanup completed`);
    } catch (error) {
      console.warn(`⚠️  Worker ${this.workerId}: People cleanup had issues but continuing:`, error);
      // Don't throw - allow tests to continue
    }
  }

  async cleanupWalls(): Promise<void> {
    console.log(`🧹 Worker ${this.workerId}: Starting walls cleanup...`);
    
    try {
      const walls = await this.getWalls();
      // Only cleanup OLD test walls (older than 1 second) for sequential execution
      const oneSecondAgo = new Date(Date.now() - 1 * 1000);
      const wallsToCleanup = walls.filter(wall => {
        // Only cleanup test walls that are old enough
        if (wall.name.includes('W') && wall.name.includes('_')) {
          // Handle both timestamp formats:
          // New format: W{worker}_T{timestamp}_... 
          // Old format: W{worker}_{6-digit-timestamp}_...
          let timestampMatch = wall.name.match(/W\d+_T(\d+)_/);
          if (timestampMatch) {
            const wallTimestamp = new Date(parseInt(timestampMatch[1]));
            return wallTimestamp < oneSecondAgo;
          }
          
          // Try old format: W{worker}_{6-digit-timestamp}_
          timestampMatch = wall.name.match(/W\d+_(\d{6})_/);
          if (timestampMatch) {
            const shortTimestamp = parseInt(timestampMatch[1]);
            const currentShortTimestamp = parseInt(Date.now().toString().slice(-6));
            // If the 6-digit timestamp is more than 1000 units old (roughly 1 second)
            return (currentShortTimestamp - shortTimestamp) > 1000;
          }
        }
        
        return false;
      });
      
      if (wallsToCleanup.length === 0) {
        console.log(`✅ Worker ${this.workerId}: No stale walls to cleanup`);
        return;
      }

      console.log(`🧹 Worker ${this.workerId}: Cleaning up ${wallsToCleanup.length} stale walls (>1sec old)`);
      
      // Delete walls sequentially to avoid race conditions
      for (const wall of wallsToCleanup) {
        try {
          await this.deleteWall(wall.id);
          console.log(`✅ Worker ${this.workerId}: Deleted stale wall ${wall.id}`);
        } catch (error) {
          // Ignore 404 errors (wall already deleted by another worker)
          if (!error.message?.includes('404')) {
            console.warn(`⚠️  Worker ${this.workerId}: Failed to cleanup wall ${wall.id}:`, error);
          }
        }
      }
      
      console.log(`✅ Worker ${this.workerId}: Walls cleanup completed`);
    } catch (error) {
      console.warn(`⚠️  Worker ${this.workerId}: Walls cleanup had issues but continuing:`, error);
      // Don't throw - allow tests to continue
    }
  }

  async cleanupAll(forceImmediate: boolean = false): Promise<void> {
    console.log(`🧹 Worker ${this.workerId}: Starting comprehensive cleanup...`);
    
    try {
      // Cleanup people and roles in parallel, but handle failures gracefully
      const cleanupPromises = [
        this.cleanupPeople(forceImmediate).catch(error => {
          console.warn(`⚠️  Worker ${this.workerId}: People cleanup failed:`, error);
          return null; // Don't fail the entire cleanup
        }),
        this.cleanupRoles(forceImmediate).catch(error => {
          console.warn(`⚠️  Worker ${this.workerId}: Roles cleanup failed:`, error);
          return null;
        }),
        this.cleanupWalls().catch(error => {
          console.warn(`⚠️  Worker ${this.workerId}: Walls cleanup failed:`, error);
          return null;
        })
      ];
      
      await Promise.allSettled(cleanupPromises);
      console.log(`✅ Worker ${this.workerId}: Comprehensive cleanup completed`);
    } catch (error) {
      console.warn(`⚠️  Worker ${this.workerId}: Cleanup had issues but continuing:`, error);
      // Don't throw - allow tests to continue
    }
  }
}
