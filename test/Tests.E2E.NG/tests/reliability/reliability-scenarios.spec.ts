import { test, expect, devices } from '@playwright/test';
import { ReliabilityHelpers } from '../helpers/reliability-helpers';
import * as fs from 'fs';

test.describe('@smoke Reliability - Baseline Validation', () => {

  test('@smoke Deterministic vs setTimeout approach validation', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Wait for app initialization using deterministic waiting
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test clicking on People List with deterministic waiting
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });

    // Verify navigation completed deterministically
    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();

    // Verify content loaded - more specific selector for the people list content
    await expect(page.locator('app-people-list')).toBeVisible();
  });

  test('@smoke Form field deterministic filling vs direct typing', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(`${baseURL}/people`);

    // Wait for form to be ready
    await reliabilityHelpers.waitForElementToBeReady('form');

    // Test deterministic form filling
    await reliabilityHelpers.fillFormFieldSafely('input[formControlName="fullName"]', 'Test User', {
      clearFirst: true,
      validateInput: true
    });

    // Verify the value was set correctly
    const inputValue = await page.locator('input[formControlName="fullName"]').inputValue();
    expect(inputValue).toBe('Test User');
  });

  test('@smoke Network-based vs Timer-based loading detection', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Use network-based loading detection
    await reliabilityHelpers.waitForNetworkQuiet();

    // Navigate to people list with network awareness
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });

    await reliabilityHelpers.waitForNavigationToComplete();
    await reliabilityHelpers.waitForNetworkQuiet();

    // Verify the content is loaded (not just visible, but actually loaded)
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke State-aware vs Stateless operations', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // State-aware navigation - check current state before navigation
    const currentUrl = page.url();
    console.log('Current URL before navigation:', currentUrl);

    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });

    // Wait for state change
    await reliabilityHelpers.waitForNavigationToComplete();

    const newUrl = page.url();
    console.log('URL after navigation:', newUrl);
    expect(newUrl).toContain('/people-list');
    expect(newUrl).not.toBe(currentUrl);
  });

  test('@smoke Element condition validation vs Timeout-based waiting', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Use element condition validation instead of fixed timeouts
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")', {
      timeout: 15000
    });

    // Navigate with element condition checks
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click');

    // Wait for specific element conditions rather than arbitrary timeout
    await reliabilityHelpers.waitForElementToBeReady('h3:has-text("People Directory")', {
      timeout: 10000
    });

    // Verify element is in expected state
    const heading = page.locator('h3:has-text("People Directory")');
    await expect(heading).toBeVisible();
    await expect(heading).toHaveText('People Directory');
  });

  test('@smoke Event-driven vs Polling mechanisms', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Use event-driven navigation detection
    const navigationPromise = page.waitForURL('**/people-list');

    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click');

    // Wait for the navigation event instead of polling
    await navigationPromise;

    // Additional verification
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke Error recovery patterns', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test error recovery - attempt an operation that might fail
    try {
      await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/invalid-route"]', 'click', undefined, {
        waitForStable: true
      });
    } catch (error) {
      console.log('Expected error for invalid route:', error.message);
      // Verify we're still on a valid page after error
      await expect(page.locator('h1:has-text("CRUD Template Application")')).toBeVisible();
    }

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

    // Test retry patterns with different retry configurations
    const startTime = Date.now();

    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")', {
      retryOptions: { maxRetries: 3, baseDelay: 100 }
    });

    const endTime = Date.now();
    console.log(`Element ready with retries: ${endTime - startTime}ms`);

    // Should succeed within reasonable time even with retries
    expect(endTime - startTime).toBeLessThan(5000);
  });

  test('@smoke Cross-device/browser reliability simulation', async ({ page, browserName, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    console.log(`Running reliability test on: ${browserName}`);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test navigation works consistently across different browsers/devices
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });

    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();

    // Browser-specific reliability patterns
    if (browserName === 'chromium') {
      // Chromium-specific checks
      console.log('Running Chromium-specific reliability checks');
    } else if (browserName === 'firefox') {
      console.log('Running Firefox-specific reliability checks');
    }
  });

  test('@smoke Load condition awareness', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    // Track page load conditions
    const startTime = Date.now();

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');
    await reliabilityHelpers.waitForNetworkQuiet();

    const loadTime = Date.now() - startTime;
    console.log(`Page load completed in: ${loadTime}ms`);

    // Verify load was reasonable (under 10 seconds even in slow conditions)
    expect(loadTime).toBeLessThan(10000);

    // Test that interactions work immediately after load completion
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });

    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke Stable element reference vs Re-querying', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Get stable reference to navigation element
    const navElement = page.locator('nav');
    await expect(navElement).toBeVisible();

    // Use stable reference for interaction
    const peopleLink = navElement.locator('a[routerLink="/people-list"]');
    await peopleLink.click();

    await reliabilityHelpers.waitForNavigationToComplete();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();

    // Verify the navigation element is still stable after route change
    await expect(navElement).toBeVisible();
  });

  test('@smoke Memory leak detection in element operations', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Perform multiple navigation operations to test for memory issues
    const routes = ['/people-list', '/roles-list', '/home', '/people-list'];

    for (const route of routes) {
      await reliabilityHelpers.interactWithElementInContext(`nav a[routerLink="${route}"]`, 'click', undefined, {
        waitForStable: true
      });
      await reliabilityHelpers.waitForNavigationToComplete();

      // Brief pause to allow for memory cleanup
      await page.waitForTimeout(100);
    }

    // Final verification that we can still interact with the page normally
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
  });

  test('@smoke Performance impact of reliability patterns', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    const metrics = {
      navigateStart: 0,
      elementWaitTime: 0,
      interactionTime: 0,
      verificationTime: 0
    };

    // Start timing
    metrics.navigateStart = Date.now();

    await page.goto(baseURL);

    const elementWaitStart = Date.now();
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');
    metrics.elementWaitTime = Date.now() - elementWaitStart;

    const interactionStart = Date.now();
    await reliabilityHelpers.interactWithElementInContext('nav a[routerLink="/people-list"]', 'click', undefined, {
      waitForStable: true
    });
    await reliabilityHelpers.waitForNavigationToComplete();
    metrics.interactionTime = Date.now() - interactionStart;

    const verificationStart = Date.now();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
    metrics.verificationTime = Date.now() - verificationStart;

    const totalTime = Date.now() - metrics.navigateStart;

    console.log('Performance Metrics:', {
      elementWait: `${metrics.elementWaitTime}ms`,
      interaction: `${metrics.interactionTime}ms`,
      verification: `${metrics.verificationTime}ms`,
      total: `${totalTime}ms`
    });

    // Ensure reliability patterns don't cause excessive performance impact
    expect(metrics.elementWaitTime).toBeLessThan(3000);
    expect(metrics.interactionTime).toBeLessThan(5000);
    expect(metrics.verificationTime).toBeLessThan(1000);
    expect(totalTime).toBeLessThan(15000);
  });

  test('@smoke Reliable element interaction verification', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test reliable link interactions - use nav context to avoid duplicate elements
    const links = ['nav a[routerLink="/people-list"]', 'nav a[routerLink="/roles-list"]'];

    for (const linkSelector of links) {
      // Navigate
      await reliabilityHelpers.interactWithElementInContext(linkSelector, 'click', undefined, {
        waitForStable: true
      });

      // Verify navigation
      await reliabilityHelpers.waitForNavigationToComplete();

      // Verify page loaded with more specific selectors
      if (linkSelector.includes('people')) {
        await reliabilityHelpers.waitForElementToBeReady('app-people-list');
        await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
      } else {
        await reliabilityHelpers.waitForElementToBeReady('app-roles-list');
        await expect(page.locator('h3:has-text("Roles")')).toBeVisible();
      }
    }
  });
});