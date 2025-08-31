// Enhanced API helpers using Polly-inspired resilience patterns for TypeScript/Playwright
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

export class ResilientApiHelpers {
  private circuitBreaker: CircuitBreaker;

  constructor(private request: APIRequestContext) {
    this.circuitBreaker = new CircuitBreaker({
      failureThreshold: 3,
      resetTimeoutMs: 30000, // 30 seconds
      monitoringPeriodMs: 60000 // 1 minute
    });
  }

  private async executeWithResilience<T>(
    operation: () => Promise<T>,
    operationName: string,
    options: Partial<RetryOptions> = {}
  ): Promise<T> {
    const retryOptions: RetryOptions = {
      maxRetries: 3,
      baseDelayMs: 500,
      maxDelayMs: 5000,
      useJitter: true,
      shouldRetry: (error) => {
        // Retry on network errors, timeouts, 5xx errors
        return error.message?.includes('timeout') || 
               error.message?.includes('ECONNRESET') ||
               error.message?.includes('500') ||
               error.message?.includes('502') ||
               error.message?.includes('503') ||
               error.message?.includes('504');
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

          const delay = this.calculateDelay(attempt, retryOptions);
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
    
    return Math.max(delay, 100); // Minimum 100ms delay
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // Enhanced API methods with resilience
  async createRole(role: TestRole): Promise<any> {
    return this.executeWithResilience(async () => {
      const response = await this.request.post('/api/roles', {
        data: role
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create role: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'createRole');
  }

  async getRoles(): Promise<any[]> {
    return this.executeWithResilience(async () => {
      const response = await this.request.get('/api/roles');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get roles: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'getRoles');
  }

  async deleteRole(id: string): Promise<void> {
    return this.executeWithResilience(async () => {
      const response = await this.request.delete(`/api/roles/${id}`);
      if (!response.ok() && response.status() !== 404) {
        throw new Error(`Failed to delete role: ${response.status()}`);
      }
    }, 'deleteRole', {
      shouldRetry: (error) => {
        // Don't retry 404s (already deleted)
        return !error.message?.includes('404');
      }
    });
  }

  async createPerson(person: TestPerson): Promise<any> {
    return this.executeWithResilience(async () => {
      const response = await this.request.post('/api/people', {
        data: person
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create person: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'createPerson');
  }

  async getPeople(): Promise<any[]> {
    return this.executeWithResilience(async () => {
      const response = await this.request.get('/api/people');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get people: ${response.status()} ${errorText}`);
      }
      return await response.json();
    }, 'getPeople');
  }

  async deletePerson(id: string): Promise<void> {
    return this.executeWithResilience(async () => {
      const response = await this.request.delete(`/api/people/${id}`);
      if (!response.ok() && response.status() !== 404) {
        throw new Error(`Failed to delete person: ${response.status()}`);
      }
    }, 'deletePerson', {
      shouldRetry: (error) => {
        return !error.message?.includes('404');
      }
    });
  }

  // Resilient cleanup with parallel operations and circuit breaker protection
  async cleanupAll(): Promise<void> {
    console.log('🧹 Starting resilient cleanup...');
    
    try {
      // Cleanup people and roles in parallel, but handle failures gracefully
      const cleanupPromises = [
        this.cleanupPeople().catch(error => {
          console.warn('⚠️  People cleanup failed:', error);
          return null; // Don't fail the entire cleanup
        }),
        this.cleanupRoles().catch(error => {
          console.warn('⚠️  Roles cleanup failed:', error);
          return null;
        })
      ];
      
      await Promise.allSettled(cleanupPromises);
      console.log('✅ Resilient cleanup completed');
    } catch (error) {
      console.warn('⚠️  Cleanup had issues but continuing:', error);
      // Don't throw - allow tests to continue
    }
  }

  private async cleanupPeople(): Promise<void> {
    const people = await this.getPeople();
    if (people.length === 0) return;

    // Delete in parallel with resilience
    const deletePromises = people.map(person => 
      this.deletePerson(person.id).catch(error => {
        console.warn(`Failed to delete person ${person.id}:`, error);
        return null;
      })
    );
    
    await Promise.allSettled(deletePromises);
  }

  private async cleanupRoles(): Promise<void> {
    const roles = await this.getRoles();
    if (roles.length === 0) return;

    const deletePromises = roles.map(role => 
      this.deleteRole(role.id).catch(error => {
        console.warn(`Failed to delete role ${role.id}:`, error);
        return null;
      })
    );
    
    await Promise.allSettled(deletePromises);
  }
}
