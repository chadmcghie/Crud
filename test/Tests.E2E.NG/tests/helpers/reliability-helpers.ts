import { Page, Locator, expect } from '@playwright/test';

/**
 * E2E Test Reliability Helpers
 *
 * Provides robust, deterministic helper functions that replace
 * flaky patterns with reliable, event-driven approaches.
 */

export class ReliabilityHelpers {
  constructor(private page: Page) {}

  /**
   * Event-driven waiting with intelligent retry patterns
   */
  async waitForElementToBeReady(
    selector: string,
    options: {
      timeout?: number;
      state?: 'visible' | 'attached' | 'detached' | 'hidden';
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<Locator> {
    const {
      timeout = 10000,
      state = 'visible',
      retryOptions = { maxRetries: 3, baseDelay: 100 }
    } = options;

    return await this.retryOperation(
      async () => {
        const element = this.page.locator(selector);
        await element.waitFor({ state, timeout });
        return element;
      },
      `waitForElementToBeReady(${selector})`,
      retryOptions
    );
  }

  /**
   * Deterministic form interactions with validation
   */
  async fillFormFieldSafely(
    selector: string,
    value: string,
    options: {
      clearFirst?: boolean;
      validateInput?: boolean;
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<void> {
    const { clearFirst = true, validateInput = true, retryOptions = { maxRetries: 3, baseDelay: 100 } } = options;

    await this.retryOperation(
      async () => {
        const field = await this.waitForElementToBeReady(selector);

        if (clearFirst) {
          await field.clear();
        }

        await field.fill(value);

        if (validateInput) {
          const actualValue = await field.inputValue();
          if (actualValue !== value) {
            throw new Error(`Form field validation failed. Expected: ${value}, Actual: ${actualValue}`);
          }
        }
      },
      `fillFormFieldSafely(${selector}, ${value})`,
      retryOptions
    );
  }

  /**
   * Robust button clicking with state verification
   */
  async clickButtonSafely(
    selector: string,
    options: {
      waitForEnabled?: boolean;
      expectedStateAfterClick?: () => Promise<boolean>;
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<void> {
    const { waitForEnabled = true, expectedStateAfterClick, retryOptions = { maxRetries: 3, baseDelay: 200 } } = options;

    await this.retryOperation(
      async () => {
        const button = await this.waitForElementToBeReady(selector);

        if (waitForEnabled) {
          await expect(button).toBeEnabled();
        }

        await button.click();

        if (expectedStateAfterClick) {
          const stateValid = await expectedStateAfterClick();
          if (!stateValid) {
            throw new Error(`Expected state not achieved after clicking ${selector}`);
          }
        }
      },
      `clickButtonSafely(${selector})`,
      retryOptions
    );
  }

  /**
   * Intelligent waiting for navigation with fallbacks
   */
  async waitForNavigationToComplete(
    expectedUrl?: string | RegExp,
    options: {
      timeout?: number;
      waitForNetworkIdle?: boolean;
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<void> {
    const { timeout = 10000, waitForNetworkIdle = false, retryOptions = { maxRetries: 2, baseDelay: 500 } } = options;

    await this.retryOperation(
      async () => {
        if (waitForNetworkIdle) {
          await this.page.waitForLoadState('networkidle', { timeout });
        } else {
          await this.page.waitForLoadState('domcontentloaded', { timeout });
        }

        if (expectedUrl) {
          await expect(this.page).toHaveURL(expectedUrl, { timeout: 5000 });
        }
      },
      'waitForNavigationToComplete',
      retryOptions
    );
  }

  /**
   * Robust API state validation with retry
   */
  async waitForApiCondition<T>(
    apiCall: () => Promise<T>,
    condition: (result: T) => boolean,
    options: {
      timeout?: number;
      pollInterval?: number;
      errorMessage?: string;
    } = {}
  ): Promise<T> {
    const { timeout = 10000, pollInterval = 250, errorMessage = 'API condition not met' } = options;
    const startTime = Date.now();

    while (Date.now() - startTime < timeout) {
      try {
        const result = await apiCall();
        if (condition(result)) {
          return result;
        }
      } catch (error) {
        // Continue polling on API errors
      }

      await this.sleep(pollInterval);
    }

    throw new Error(`${errorMessage} within ${timeout}ms`);
  }

  /**
   * Deterministic data validation with expect.toPass
   */
  async validateDataConsistency<T>(
    dataProvider: () => Promise<T>,
    validator: (data: T) => void,
    options: {
      timeout?: number;
      intervals?: number[];
      errorMessage?: string;
    } = {}
  ): Promise<void> {
    const { timeout = 5000, intervals = [100, 250, 500], errorMessage = 'Data consistency validation failed' } = options;

    await expect(async () => {
      const data = await dataProvider();
      validator(data);
    }).toPass({
      timeout,
      intervals
    });
  }

  /**
   * Smart element interaction with context awareness
   */
  async interactWithElementInContext(
    selector: string,
    action: 'click' | 'fill' | 'check' | 'uncheck',
    value?: string,
    options: {
      context?: string; // Parent context selector
      waitForStable?: boolean;
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<void> {
    const { context, waitForStable = true, retryOptions = { maxRetries: 3, baseDelay: 100 } } = options;

    await this.retryOperation(
      async () => {
        let element: Locator;

        if (context) {
          const contextElement = await this.waitForElementToBeReady(context);
          element = contextElement.locator(selector);
        } else {
          element = this.page.locator(selector);
        }

        if (waitForStable) {
          // Wait for element to be stable (not moving/changing)
          await element.waitFor({ state: 'visible' });
          await this.page.waitForTimeout(50); // Small buffer for stability
        }

        switch (action) {
          case 'click':
            await element.click();
            break;
          case 'fill':
            if (value === undefined) throw new Error('Value required for fill action');
            await element.fill(value);
            break;
          case 'check':
            await element.check();
            break;
          case 'uncheck':
            await element.uncheck();
            break;
        }
      },
      `interactWithElementInContext(${selector}, ${action})`,
      retryOptions
    );
  }

  /**
   * Comprehensive error recovery patterns
   */
  async handlePotentialDialogs(
    operation: () => Promise<void>,
    dialogHandlers: {
      alert?: (message: string) => Promise<void>;
      confirm?: (message: string) => Promise<boolean>;
      prompt?: (message: string, defaultValue?: string) => Promise<string | null>;
    } = {}
  ): Promise<void> {
    // Set up dialog handlers
    if (dialogHandlers.alert) {
      this.page.on('dialog', async (dialog) => {
        if (dialog.type() === 'alert') {
          await dialogHandlers.alert!(dialog.message());
          await dialog.accept();
        }
      });
    }

    if (dialogHandlers.confirm) {
      this.page.on('dialog', async (dialog) => {
        if (dialog.type() === 'confirm') {
          const shouldAccept = await dialogHandlers.confirm!(dialog.message());
          if (shouldAccept) {
            await dialog.accept();
          } else {
            await dialog.dismiss();
          }
        }
      });
    }

    if (dialogHandlers.prompt) {
      this.page.on('dialog', async (dialog) => {
        if (dialog.type() === 'prompt') {
          const response = await dialogHandlers.prompt!(dialog.message(), dialog.defaultValue());
          if (response !== null) {
            await dialog.accept(response);
          } else {
            await dialog.dismiss();
          }
        }
      });
    }

    await operation();

    // Clean up dialog handlers
    this.page.removeAllListeners('dialog');
  }

  /**
   * Network resilience patterns
   */
  async executeWithNetworkResilience<T>(
    operation: () => Promise<T>,
    options: {
      retryOnNetworkError?: boolean;
      retryOptions?: RetryOptions;
    } = {}
  ): Promise<T> {
    const { retryOnNetworkError = true, retryOptions = { maxRetries: 3, baseDelay: 1000 } } = options;

    if (!retryOnNetworkError) {
      return await operation();
    }

    return await this.retryOperation(
      operation,
      'executeWithNetworkResilience',
      {
        ...retryOptions,
        shouldRetry: (error) => {
          const errorMessage = error.message.toLowerCase();
          return errorMessage.includes('network') ||
                 errorMessage.includes('timeout') ||
                 errorMessage.includes('connection') ||
                 errorMessage.includes('failed to fetch');
        }
      }
    );
  }

  /**
   * Robust retry mechanism with intelligent backoff
   */
  async retryOperation<T>(
    operation: () => Promise<T>,
    operationName: string,
    options: RetryOptions = {}
  ): Promise<T> {
    const {
      maxRetries = 3,
      baseDelay = 500,
      maxDelay = 5000,
      useExponentialBackoff = true,
      shouldRetry = () => true,
      onRetry
    } = options;

    let lastError: Error;

    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error as Error;

        if (attempt === maxRetries || !shouldRetry(lastError)) {
          break;
        }

        const delay = useExponentialBackoff
          ? Math.min(baseDelay * Math.pow(2, attempt), maxDelay)
          : baseDelay;

        if (onRetry) {
          await onRetry(attempt + 1, lastError, delay);
        }

        console.log(`Retry ${attempt + 1}/${maxRetries} for ${operationName} after ${delay}ms delay`);
        await this.sleep(delay);
      }
    }

    throw new Error(`Operation ${operationName} failed after ${maxRetries + 1} attempts. Last error: ${lastError.message}`);
  }

  /**
   * State management utilities
   */
  async capturePageState(): Promise<PageState> {
    return {
      url: this.page.url(),
      title: await this.page.title(),
      timestamp: Date.now(),
      localStorageData: await this.page.evaluate(() => ({ ...localStorage })),
      sessionStorageData: await this.page.evaluate(() => ({ ...sessionStorage }))
    };
  }

  async restorePageState(state: PageState): Promise<void> {
    if (this.page.url() !== state.url) {
      await this.page.goto(state.url);
    }

    await this.page.evaluate((storageData) => {
      // Restore localStorage
      Object.entries(storageData.localStorage).forEach(([key, value]) => {
        localStorage.setItem(key, value);
      });

      // Restore sessionStorage
      Object.entries(storageData.sessionStorage).forEach(([key, value]) => {
        sessionStorage.setItem(key, value);
      });
    }, {
      localStorage: state.localStorageData,
      sessionStorage: state.sessionStorageData
    });
  }

  /**
   * Wait for network activity to be quiet (no active requests)
   */
  async waitForNetworkQuiet(options: {
    timeout?: number;
    idleTime?: number;
  } = {}): Promise<void> {
    const { timeout = 10000, idleTime = 500 } = options;

    try {
      await this.page.waitForLoadState('networkidle', { timeout });
    } catch (error) {
      // Fallback: wait for a short period if networkidle times out
      console.log(`Network idle timeout, using fallback delay: ${idleTime}ms`);
      await this.sleep(idleTime);
    }
  }

  /**
   * Utility methods
   */
  private async sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Type definitions
interface RetryOptions {
  maxRetries?: number;
  baseDelay?: number;
  maxDelay?: number;
  useExponentialBackoff?: boolean;
  shouldRetry?: (error: Error) => boolean;
  onRetry?: (attempt: number, error: Error, delay: number) => Promise<void>;
}

interface PageState {
  url: string;
  title: string;
  timestamp: number;
  localStorageData: Record<string, string>;
  sessionStorageData: Record<string, string>;
}

export { RetryOptions, PageState };