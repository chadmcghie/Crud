/**
 * Angular Wait Helpers - Unified patterns for waiting on Angular application state
 *
 * This module provides consistent, observable-like patterns for waiting on Angular
 * application readiness, replacing inconsistent wait patterns across tests.
 */

import { Page } from '@playwright/test';

export interface AngularReadinessOptions {
  timeout?: number;
  checkInterval?: number;
  requireDataReady?: boolean;
  logDebug?: boolean;
}

/**
 * Observable-like pattern for Angular readiness
 * Returns a promise that resolves when Angular is fully ready
 */
export class AngularReadinessObserver {
  private page: Page;
  private options: Required<AngularReadinessOptions>;

  constructor(page: Page, options: AngularReadinessOptions = {}) {
    this.page = page;
    this.options = {
      timeout: options.timeout || (process.env.CI ? 30000 : 15000),
      checkInterval: options.checkInterval || 100,
      requireDataReady: options.requireDataReady ?? true,
      logDebug: options.logDebug ?? (process.env.DEBUG_E2E === 'true')
    };
  }

  /**
   * Wait for Angular to be fully ready
   * Checks multiple conditions to ensure the app is stable
   */
  async waitForReady(): Promise<void> {
    const startTime = Date.now();

    if (this.options.logDebug) {
      console.log('[AngularReady] Starting readiness check...');
    }

    // Step 1: Wait for Angular framework to be loaded
    await this.waitForAngularFramework();

    // Step 2: Wait for Angular to be stable (no pending operations)
    await this.waitForAngularStability();

    // Step 3: Wait for router to be ready
    await this.waitForRouterReady();

    // Step 4: If required, wait for data to be loaded
    if (this.options.requireDataReady) {
      await this.waitForDataReady();
    }

    const elapsed = Date.now() - startTime;
    if (this.options.logDebug) {
      console.log(`[AngularReady] Application ready in ${elapsed}ms`);
    }
  }

  /**
   * Wait for Angular framework to be present
   */
  private async waitForAngularFramework(): Promise<void> {
    await this.page.waitForFunction(
      () => {
        // Check if Angular is defined
        const hasAngular = typeof (window as any).ng !== 'undefined';
        // Check if Angular modules are loaded
        const hasAllAngularModules = typeof (window as any).getAllAngularRootElements === 'function' ||
                                      typeof (window as any).ng?.probe === 'function';
        return hasAngular || hasAllAngularModules;
      },
      { timeout: this.options.timeout }
    );
  }

  /**
   * Wait for Angular to be stable (no pending HTTP requests or async operations)
   */
  private async waitForAngularStability(): Promise<void> {
    await this.page.waitForFunction(
      () => {
        // For Angular 9+, check if the app is stable
        const ngRef = (window as any).ng;
        if (ngRef && ngRef.getComponent) {
          try {
            // Try to get the root component - if successful, Angular is stable
            const rootElement = document.querySelector('app-root');
            if (rootElement) {
              const component = ngRef.getComponent(rootElement);
              return component !== null && component !== undefined;
            }
          } catch (e) {
            // Component not ready yet
            return false;
          }
        }

        // Fallback: Check for Angular's presence
        return typeof (window as any).ng !== 'undefined';
      },
      { timeout: this.options.timeout }
    );

    // Additional stability check - wait for no pending network requests
    try {
      await this.page.waitForLoadState('networkidle', { timeout: 5000 });
    } catch {
      // Network might still have activity, but continue if Angular is ready
    }
  }

  /**
   * Wait for Angular Router to be ready
   */
  private async waitForRouterReady(): Promise<void> {
    await this.page.waitForFunction(
      () => {
        // Check if router outlets are present and navigation links are ready
        const hasRouterOutlet = document.querySelector('router-outlet') !== null;
        const hasNavigationLinks = document.querySelector('a[routerLink]') !== null;
        return hasRouterOutlet || hasNavigationLinks;
      },
      { timeout: this.options.timeout }
    );
  }

  /**
   * Wait for initial data to be loaded (tables, lists, etc.)
   */
  private async waitForDataReady(): Promise<void> {
    await this.page.waitForFunction(
      () => {
        // Check for common data indicators
        const hasTables = document.querySelector('table tbody tr') !== null;
        const hasLists = document.querySelector('ul li, ol li') !== null;
        const hasCards = document.querySelector('.card, [class*="card"]') !== null;
        const hasEmptyState = document.querySelector('[class*="empty"], [class*="no-data"], :has-text("No ")') !== null;

        // Data is ready if we have content OR an empty state
        return hasTables || hasLists || hasCards || hasEmptyState;
      },
      { timeout: Math.min(this.options.timeout / 2, 10000) } // Shorter timeout for data
    ).catch(() => {
      // Data might not be required for this test
      if (this.options.logDebug) {
        console.log('[AngularReady] No data elements found, continuing...');
      }
    });
  }
}

/**
 * Convenience function for waiting for Angular readiness
 */
export async function waitForAngularReady(
  page: Page,
  options: AngularReadinessOptions = {}
): Promise<void> {
  const observer = new AngularReadinessObserver(page, options);
  await observer.waitForReady();
}

/**
 * Wait for a specific Angular component to be ready
 */
export async function waitForComponentReady(
  page: Page,
  componentSelector: string,
  options: { timeout?: number } = {}
): Promise<void> {
  const timeout = options.timeout || (process.env.CI ? 20000 : 10000);

  // Wait for the component to be in the DOM
  await page.locator(componentSelector).waitFor({
    state: 'attached',
    timeout
  });

  // Wait for Angular to be ready
  await waitForAngularReady(page, { requireDataReady: false });

  // Wait for the component to be visible and stable
  await page.locator(componentSelector).waitFor({
    state: 'visible',
    timeout
  });
}

/**
 * Wait for navigation to complete
 */
export async function waitForNavigationComplete(
  page: Page,
  options: { timeout?: number } = {}
): Promise<void> {
  const timeout = options.timeout || (process.env.CI ? 30000 : 15000);

  // Wait for URL to change (if applicable)
  await page.waitForLoadState('domcontentloaded', { timeout });

  // Wait for Angular to be ready
  await waitForAngularReady(page, { timeout });
}

/**
 * Observable-like subscription pattern for monitoring Angular state changes
 */
export class AngularStateMonitor {
  private page: Page;
  private listeners: Map<string, Function[]> = new Map();

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Subscribe to route changes
   */
  onRouteChange(callback: (url: string) => void): () => void {
    const listener = (url: string) => callback(url);

    if (!this.listeners.has('route')) {
      this.listeners.set('route', []);

      // Start monitoring route changes
      this.page.on('framenavigated', (frame) => {
        if (frame === this.page.mainFrame()) {
          const url = frame.url();
          this.listeners.get('route')?.forEach(fn => fn(url));
        }
      });
    }

    this.listeners.get('route')!.push(listener);

    // Return unsubscribe function
    return () => {
      const listeners = this.listeners.get('route');
      if (listeners) {
        const index = listeners.indexOf(listener);
        if (index > -1) {
          listeners.splice(index, 1);
        }
      }
    };
  }

  /**
   * Subscribe to component lifecycle events
   */
  async onComponentReady(selector: string, callback: () => void): Promise<() => void> {
    const observer = await this.page.evaluateHandle((selector) => {
      const targetNode = document.querySelector(selector);
      if (!targetNode) return null;

      const observer = new MutationObserver(() => {
        // Component DOM changed
        window.dispatchEvent(new CustomEvent('component-ready', { detail: selector }));
      });

      observer.observe(targetNode, {
        childList: true,
        subtree: true,
        attributes: true
      });

      return observer;
    }, selector);

    const listener = (event: any) => {
      if (event.detail === selector) {
        callback();
      }
    };

    await this.page.evaluateHandle(() => {
      window.addEventListener('component-ready', listener as any);
    });

    // Return unsubscribe function
    return async () => {
      if (observer) {
        await observer.evaluate((obs: any) => obs?.disconnect());
      }
    };
  }
}

/**
 * Retry helper with observable-like pattern
 */
export async function retryUntilReady<T>(
  operation: () => Promise<T>,
  readinessCheck: (result: T) => boolean,
  options: {
    maxRetries?: number;
    retryDelay?: number;
    timeout?: number;
  } = {}
): Promise<T> {
  const maxRetries = options.maxRetries || 10;
  const retryDelay = options.retryDelay || 500;
  const timeout = options.timeout || 30000;
  const startTime = Date.now();

  for (let i = 0; i < maxRetries; i++) {
    if (Date.now() - startTime > timeout) {
      throw new Error(`Operation timed out after ${timeout}ms`);
    }

    try {
      const result = await operation();
      if (readinessCheck(result)) {
        return result;
      }
    } catch (error) {
      if (i === maxRetries - 1) {
        throw error;
      }
    }

    await new Promise(resolve => setTimeout(resolve, retryDelay));
  }

  throw new Error(`Operation failed after ${maxRetries} retries`);
}