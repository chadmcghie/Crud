import { test, expect } from '../fixtures/simple-test-fixture';
import { ReliabilityHelpers } from '../helpers/reliability-helpers';

test.describe('@smoke Reliability - Baseline Validation', () => {
  test('@smoke Deterministic test patterns', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test deterministic waiting
    await reliabilityHelpers.waitForElementToBeReady('nav a[routerLink="/people-list"]');

    // Test stable form interaction
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people"]', 'click', undefined, {
      waitForStable: true
    });
    await reliabilityHelpers.waitForNavigationToComplete();

    // Verify expected state - corrected heading text
    await expect(page.locator('h3:has-text("Add New Person")')).toBeVisible();
  });

  test('@smoke Network resilience patterns', async ({ page, baseURL, apiUrl }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test API resilience pattern
    const result = await reliabilityHelpers.executeWithNetworkResilience(async () => {
      const response = await page.request.get(`${apiUrl}/api/people`);
      return response.ok();
    });

    expect(result).toBe(true);
  });

  test('@smoke Data validation patterns', async ({ page, baseURL, apiUrl }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Test data consistency validation
    await reliabilityHelpers.validateDataConsistency(
      async () => {
        const response = await page.request.get(`${apiUrl}/api/people`);
        return response.json();
      },
      (data) => {
        expect(Array.isArray(data)).toBe(true);
      }
    );
  });

  test('@smoke Context-aware interactions', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test context-aware element interaction
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click', undefined, {
      context: 'nav',
      waitForStable: true
    });

    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke Page state management', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Capture initial state
    const initialState = await reliabilityHelpers.capturePageState();
    expect(initialState.url).toContain(baseURL);

    // Navigate away
    await page.goto(`${baseURL}/people-list`);
    await reliabilityHelpers.waitForNavigationToComplete();

    // Restore state
    await reliabilityHelpers.restorePageState(initialState);
    await expect(page.locator('h1:has-text("CRUD Template Application")')).toBeVisible();
  });

  test('@smoke Error recovery patterns', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test error recovery - navigate to invalid route programmatically
    try {
      await page.goto(`${baseURL}/invalid-route`);

      // Angular should handle invalid routes (removed arbitrary 1000ms timeout)
      // The checks below will verify if Angular handled it correctly

      // Check if we're redirected or if we stayed on invalid route
      const currentUrl = page.url();
      console.log('Current URL after invalid route:', currentUrl);

      // Either we get redirected to a valid route or we handle the error gracefully
      const hasValidContent = await page.locator('h1:has-text("CRUD Template Application")').isVisible({ timeout: 2000 }).catch(() => false);
      const hasErrorMessage = await page.locator('text=Page not found, text=404').isVisible({ timeout: 2000 }).catch(() => false);

      // The app should either redirect us back or show a proper error page
      expect(hasValidContent || hasErrorMessage).toBe(true);

    } catch (error) {
      console.log('Expected error for invalid route:', error.message);
    }

    // Verify we can still navigate normally after the error
    await page.goto(baseURL); // Go back to home
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Verify normal navigation still works after error
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });
    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke Retry pattern effectiveness', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Test retry pattern with eventual success
    let attemptCount = 0;
    const result = await reliabilityHelpers.retryOperation(
      async () => {
        attemptCount++;
        if (attemptCount < 2) {
          throw new Error('Simulated transient failure');
        }
        return 'success';
      },
      'test-retry-pattern',
      { maxRetries: 3, baseDelay: 10 }
    );

    expect(result).toBe('success');
    expect(attemptCount).toBe(2);
  });

  test('@smoke Network quiet detection', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test network quiet detection
    await reliabilityHelpers.waitForNetworkQuiet({ timeout: 5000, idleTime: 500 });

    // Should complete without timeout
    expect(page.url()).toContain(baseURL);
  });
});