import { Page, APIRequestContext } from '@playwright/test';

/**
 * Performance-Optimized E2E Test Helpers
 *
 * Provides fast, efficient helper functions that minimize
 * UI interactions and optimize test execution time.
 */

export class PerformanceOptimizedHelpers {
  constructor(
    private page: Page,
    private request: APIRequestContext,
    private apiUrl: string
  ) {}

  /**
   * Fast data setup using API instead of UI
   */
  async createTestPerson(data?: Partial<TestPerson>): Promise<TestPerson> {
    const personData = {
      fullName: data?.fullName || `Test User ${Date.now()}`,
      phone: data?.phone || '+1-555-0000',
      ...data
    };

    const response = await this.request.post(`${this.apiUrl}/api/people`, {
      data: personData
    });

    if (!response.ok()) {
      throw new Error(`Failed to create test person: ${response.status()}`);
    }

    return await response.json();
  }

  async createTestRole(data?: Partial<TestRole>): Promise<TestRole> {
    const roleData = {
      name: data?.name || `Test Role ${Date.now()}`,
      description: data?.description || 'Test role for E2E testing',
      ...data
    };

    const response = await this.request.post(`${this.apiUrl}/api/roles`, {
      data: roleData
    });

    if (!response.ok()) {
      throw new Error(`Failed to create test role: ${response.status()}`);
    }

    return await response.json();
  }

  /**
   * Batch creation for multiple test entities
   */
  async createMultiplePeople(count: number): Promise<TestPerson[]> {
    const promises = Array.from({ length: count }, (_, i) =>
      this.createTestPerson({ fullName: `Batch User ${i + 1}`, phone: `+1-555-000${i}` })
    );

    return Promise.all(promises);
  }

  /**
   * Fast cleanup using API
   */
  async cleanupTestPerson(id: string): Promise<void> {
    await this.request.delete(`${this.apiUrl}/api/people/${id}`);
  }

  async cleanupTestRole(id: string): Promise<void> {
    await this.request.delete(`${this.apiUrl}/api/roles/${id}`);
  }

  async cleanupMultiplePeople(ids: string[]): Promise<void> {
    const promises = ids.map(id => this.cleanupTestPerson(id));
    await Promise.all(promises);
  }

  /**
   * Optimized navigation with caching
   */
  private navigationCache = new Map<string, boolean>();

  async navigateToModule(module: 'people' | 'roles'): Promise<void> {
    const cacheKey = `${this.page.url()}-${module}`;

    if (this.navigationCache.get(cacheKey)) {
      return; // Already on the correct page
    }

    const routeMap = {
      people: '/people-list',
      roles: '/roles-list'
    };

    const link = this.page.locator(`a[routerLink="${routeMap[module]}"]`);
    await link.click();

    const componentMap = {
      people: 'app-people-list',
      roles: 'app-roles-list'
    };

    await this.page.locator(componentMap[module]).waitFor({
      state: 'visible',
      timeout: 5000
    });

    this.navigationCache.set(cacheKey, true);
  }

  /**
   * Optimized form filling with minimal UI interaction
   */
  async fillPersonFormFast(data: Partial<TestPerson>): Promise<void> {
    // Use JavaScript execution for faster form filling
    await this.page.evaluate((formData) => {
      const nameInput = document.querySelector<HTMLInputElement>('#fullName, input[name="fullName"]');
      const phoneInput = document.querySelector<HTMLInputElement>('#phone, input[name="phone"]');

      if (nameInput && formData.fullName) {
        nameInput.value = formData.fullName;
        nameInput.dispatchEvent(new Event('input', { bubbles: true }));
      }

      if (phoneInput && formData.phone) {
        phoneInput.value = formData.phone;
        phoneInput.dispatchEvent(new Event('input', { bubbles: true }));
      }
    }, data);
  }

  /**
   * Fast wait strategies with exponential backoff
   */
  async waitForApiResponse(endpoint: string, expectedStatus: number = 200): Promise<boolean> {
    const maxRetries = 5;
    let delay = 100;

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        const response = await this.request.get(`${this.apiUrl}${endpoint}`);
        if (response.status() === expectedStatus) {
          return true;
        }
      } catch (error) {
        // Continue retrying
      }

      await this.sleep(delay);
      delay = Math.min(delay * 2, 2000); // Cap at 2 seconds
    }

    return false;
  }

  async waitForElementWithRetry(selector: string, timeout: number = 5000): Promise<boolean> {
    try {
      await this.page.locator(selector).waitFor({ state: 'visible', timeout });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Optimized state validation
   */
  async validatePersonInList(personName: string): Promise<boolean> {
    // Use JavaScript for faster DOM query
    return await this.page.evaluate((name) => {
      const rows = document.querySelectorAll('tr, .person-item');
      return Array.from(rows).some(row => row.textContent?.includes(name));
    }, personName);
  }

  /**
   * Performance monitoring utilities
   */
  async measureOperationTime<T>(operation: () => Promise<T>, operationName: string): Promise<T> {
    const startTime = Date.now();
    try {
      const result = await operation();
      const endTime = Date.now();
      console.log(`Operation "${operationName}" took ${endTime - startTime}ms`);
      return result;
    } catch (error) {
      const endTime = Date.now();
      console.log(`Operation "${operationName}" failed after ${endTime - startTime}ms`);
      throw error;
    }
  }

  async getPageLoadMetrics(): Promise<PerformanceMetrics> {
    return await this.page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      return {
        domContentLoaded: Math.round(navigation.domContentLoadedEventEnd - navigation.navigationStart),
        loadComplete: Math.round(navigation.loadEventEnd - navigation.navigationStart),
        firstPaint: Math.round((performance.getEntriesByType('paint').find(entry =>
          entry.name === 'first-paint')?.startTime || 0)),
        firstContentfulPaint: Math.round((performance.getEntriesByType('paint').find(entry =>
          entry.name === 'first-contentful-paint')?.startTime || 0))
      };
    });
  }

  /**
   * Database optimization helpers
   */
  async resetDatabaseFast(): Promise<void> {
    // Use API reset endpoint if available
    try {
      await this.request.post(`${this.apiUrl}/api/test/reset`, {
        headers: { 'X-Test-Reset-Token': 'test-only-token' }
      });
    } catch {
      // Fallback to manual cleanup if reset endpoint not available
      console.log('Database reset endpoint not available, using manual cleanup');
    }
  }

  /**
   * Utility methods
   */
  private async sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Test data builders for fast setup
   */
  buildTestPerson(overrides?: Partial<TestPerson>): Partial<TestPerson> {
    const timestamp = Date.now();
    return {
      fullName: `Performance Test User ${timestamp}`,
      phone: `+1-555-${String(timestamp).slice(-4)}`,
      ...overrides
    };
  }

  buildTestRole(overrides?: Partial<TestRole>): Partial<TestRole> {
    const timestamp = Date.now();
    return {
      name: `Performance Test Role ${timestamp}`,
      description: `Role created for performance testing at ${new Date().toISOString()}`,
      ...overrides
    };
  }
}

// Type definitions
interface TestPerson {
  id?: string;
  fullName: string;
  phone: string;
}

interface TestRole {
  id?: string;
  name: string;
  description: string;
}

interface PerformanceMetrics {
  domContentLoaded: number;
  loadComplete: number;
  firstPaint: number;
  firstContentfulPaint: number;
}

export { TestPerson, TestRole, PerformanceMetrics };