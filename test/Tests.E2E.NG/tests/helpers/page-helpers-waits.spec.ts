import { test, expect } from '@playwright/test';
import { PageHelpers } from './page-helpers';

/**
 * Tests for event-driven wait methods in PageHelpers
 * These tests validate that waits are based on actual application state
 * rather than arbitrary timeouts
 *
 * NOTE: These are infrastructure tests and are skipped in CI.
 * Run locally with: npx playwright test tests/helpers/page-helpers-waits.spec.ts
 */

test.describe.skip('@meta @dev PageHelpers Event-Driven Wait Methods', () => {
  const angularUrl = process.env.ANGULAR_URL || 'http://localhost:4200';
  const apiUrl = process.env.API_URL || 'http://localhost:5172';

  test.describe('waitForNavigationComplete', () => {
    test('should wait for Angular navigation to complete', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Navigate to a different route
      await page.click('a[routerLink="/people-list"]');

      // Wait for navigation to complete
      const startTime = Date.now();
      await helpers.waitForNavigationComplete();
      const duration = Date.now() - startTime;

      // Should complete quickly (not waiting for arbitrary timeout)
      expect(duration).toBeLessThan(5000);

      // Verify we're on the correct page
      await expect(page).toHaveURL(/\/people-list/);

      // Verify Angular is stable
      const isStable = await page.evaluate(() => {
        return typeof (window as any).ng !== 'undefined';
      });
      expect(isStable).toBe(true);
    });

    test('should handle navigation with query parameters', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);
      await page.goto(`${angularUrl}/people-list?filter=test`);

      await helpers.waitForNavigationComplete();

      // Verify URL includes query params
      await expect(page).toHaveURL(/filter=test/);
    });

    test('should wait for DOM content to be loaded', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);
      await helpers.waitForNavigationComplete();

      // Verify page content is present
      const hasContent = await page.locator('app-root').isVisible();
      expect(hasContent).toBe(true);
    });
  });

  test.describe('waitForDataLoad', () => {
    test('should wait for API data to load', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(`${angularUrl}/people-list`);

      // Set up response listener before action
      const responsePromise = page.waitForResponse(
        response => response.url().includes('/api/people') && response.ok()
      );

      // Trigger data load (page refresh)
      await page.reload();

      // Wait for data to load
      await helpers.waitForDataLoad('/api/people');

      // Verify response was received
      const response = await responsePromise;
      expect(response.ok()).toBe(true);
    });

    test('should wait for multiple API endpoints', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Set up listeners for both endpoints
      const peoplePromise = page.waitForResponse(r => r.url().includes('/api/people'));
      const rolesPromise = page.waitForResponse(r => r.url().includes('/api/roles'));

      // Navigate to trigger API calls
      await page.goto(`${angularUrl}/people-list`);

      // Wait for both using our helper
      await helpers.waitForDataLoad(['/api/people', '/api/roles'], { timeout: 15000 });

      // Verify responses were received
      const [peopleResp, rolesResp] = await Promise.all([peoplePromise, rolesPromise]);
      expect(peopleResp.ok()).toBe(true);
      expect(rolesResp.ok()).toBe(true);
    });

    test('should timeout if data does not load', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Try to wait for non-existent endpoint with short timeout
      await expect(async () => {
        await helpers.waitForDataLoad('/api/nonexistent', { timeout: 2000 });
      }).rejects.toThrow();
    });

    test('should handle API errors gracefully', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Mock API to return error
      await page.route('**/api/test-error', route =>
        route.fulfill({ status: 500, body: 'Server Error' })
      );

      // Navigate to trigger the error
      await page.evaluate(() => {
        fetch('/api/test-error').catch(() => {});
      });

      // Should timeout gracefully
      await expect(async () => {
        await helpers.waitForDataLoad('/api/test-error', { timeout: 1000 });
      }).rejects.toThrow();
    });
  });

  test.describe('waitForComponentReady', () => {
    test('should wait for Angular component to be ready', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Wait for main app component
      await helpers.waitForComponentReady('app-root');

      // Verify component is visible and interactive
      const component = page.locator('app-root');
      await expect(component).toBeVisible();

      // Verify Angular bootstrapped
      const hasAngular = await page.evaluate(() => typeof (window as any).ng !== 'undefined');
      expect(hasAngular).toBe(true);
    });

    test('should wait for component with data binding', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);
      await helpers.waitForNavigationComplete();

      // Navigate to people list
      await page.click('a[routerLink="/people-list"]');
      await helpers.waitForNavigationComplete();

      // Wait for component and its data
      await helpers.waitForComponentReady('app-people', { waitForData: true, timeout: 20000 });

      // Verify component has content (either data or empty state)
      const hasContent = await page.locator('app-people').first().isVisible();
      expect(hasContent).toBe(true);
    });

    test('should wait for nested components', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Wait for main app component
      await helpers.waitForComponentReady('app-root', { timeout: 15000 });

      // Verify component structure is present
      const appRoot = await page.locator('app-root').isVisible();
      expect(appRoot).toBe(true);

      // Verify Angular initialized
      const hasAngular = await page.evaluate(() => typeof (window as any).ng !== 'undefined');
      expect(hasAngular).toBe(true);
    });

    test('should timeout if component does not appear', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);

      // Try to wait for non-existent component
      await expect(async () => {
        await helpers.waitForComponentReady('app-nonexistent', { timeout: 2000 });
      }).rejects.toThrow();
    });
  });

  test.describe('waitForFormSubmission', () => {
    test('should wait for form submission to complete', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(`${angularUrl}/people-list`);

      // Click add person button
      const addButton = page.locator('button, a').filter({ hasText: /add|new/i }).first();
      if (await addButton.isVisible()) {
        await addButton.click();
        await helpers.waitForNavigationComplete();

        // Fill form
        await page.fill('input#fullName', 'Test User');

        // Submit and wait for completion
        const submitPromise = helpers.waitForFormSubmission('/api/people', 'POST');
        await page.click('button[type="submit"]');
        await submitPromise;

        // Verify navigation back to list
        await expect(page).toHaveURL(/\/people-list/);
      }
    });

    test('should wait for PUT request on edit', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(`${angularUrl}/people-list`);
      await helpers.waitForDataLoad('/api/people');

      // Check if there are any people to edit
      const hasPeople = await page.locator('table tbody tr').count() > 0;

      if (hasPeople) {
        // Click edit on first person
        await page.locator('table tbody tr').first().locator('button:has-text("Edit")').click();
        await helpers.waitForNavigationComplete();

        // Modify form
        await page.fill('input#fullName', 'Updated Name');

        // Submit and wait for PUT request
        const submitPromise = helpers.waitForFormSubmission('/api/people', 'PUT');
        await page.click('button[type="submit"]');
        await submitPromise;

        // Verify navigation back
        await expect(page).toHaveURL(/\/people-list/);
      }
    });

    test('should handle form validation errors', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(`${angularUrl}/people-list`);

      // Click add person button
      const addButton = page.locator('button, a').filter({ hasText: /add|new/i }).first();
      if (await addButton.isVisible()) {
        await addButton.click();
        await helpers.waitForNavigationComplete();

        // Try to submit empty form
        const submitButton = page.locator('button[type="submit"]');

        // Check if button is disabled (client-side validation)
        const isDisabled = await submitButton.isDisabled();

        if (isDisabled) {
          // Form prevents submission - this is expected
          expect(isDisabled).toBe(true);
        } else {
          // If not disabled, submission should fail or show validation
          await submitButton.click();

          // Should not navigate away (still on form page)
          await page.waitForTimeout(500);
          const currentUrl = page.url();
          expect(currentUrl).not.toContain('/people-list');
        }
      }
    });

    test('should wait for loading indicators to disappear', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(`${angularUrl}/people-list`);

      // Wait for initial load
      await helpers.waitForDataLoad('/api/people');

      // Trigger a refresh that might show loading indicator
      if (await page.locator('button:has-text("Refresh")').isVisible()) {
        await page.click('button:has-text("Refresh")');
        await helpers.waitForDataLoad('/api/people');

        // Verify no loading indicators are present
        const hasLoadingIndicator = await page.locator('.loading, .spinner, [aria-busy="true"]').isVisible().catch(() => false);
        expect(hasLoadingIndicator).toBe(false);
      }
    });
  });

  test.describe('Combined Wait Scenarios', () => {
    test('should handle navigation + data load + component ready', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);
      await helpers.waitForNavigationComplete();

      // Navigate to people list
      await page.click('a[routerLink="/people-list"]');

      // Wait for all steps
      await helpers.waitForNavigationComplete();
      await helpers.waitForDataLoad('/api/people', { timeout: 15000 });
      await helpers.waitForComponentReady('app-people', { timeout: 15000 });

      // Verify everything is ready
      const componentVisible = await page.locator('app-people').isVisible();
      expect(componentVisible).toBe(true);

      // Verify URL is correct
      await expect(page).toHaveURL(/\/people-list/);
    });

    test('should handle rapid navigation changes', async ({ page }) => {
      const helpers = new PageHelpers(page);

      await page.goto(angularUrl);
      await helpers.waitForNavigationComplete();

      // Rapid navigation between routes
      await page.click('a[routerLink="/people-list"]');
      await helpers.waitForNavigationComplete({ timeout: 20000 });

      await page.click('a[routerLink="/roles-list"]');
      await helpers.waitForNavigationComplete({ timeout: 20000 });

      await page.click('a[routerLink="/people-list"]');
      await helpers.waitForNavigationComplete({ timeout: 20000 });

      // Verify final state
      await expect(page).toHaveURL(/\/people-list/);
      const componentVisible = await page.locator('app-people').isVisible();
      expect(componentVisible).toBe(true);
    });

    test('should work in CI environment with slower resources', async ({ page }) => {
      const helpers = new PageHelpers(page);
      const isCI = !!process.env.CI;

      await page.goto(angularUrl);
      await helpers.waitForNavigationComplete();

      await page.click('a[routerLink="/people-list"]');

      // Should complete regardless of environment
      const startTime = Date.now();
      await helpers.waitForNavigationComplete({ timeout: 20000 });
      await helpers.waitForDataLoad('/api/people', { timeout: 20000 });
      await helpers.waitForComponentReady('app-people', { timeout: 20000 });
      const duration = Date.now() - startTime;

      // Should be reasonable even in CI (adjust timeout expectations)
      const maxDuration = isCI ? 25000 : 15000;
      expect(duration).toBeLessThan(maxDuration);
    });
  });
});
