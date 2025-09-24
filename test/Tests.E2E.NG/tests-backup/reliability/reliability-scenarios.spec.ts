import { test, expect } from '../fixtures/serial-test-fixture';
import { ReliabilityHelpers } from '../helpers/reliability-helpers';
import { PerformanceOptimizedHelpers } from '../helpers/performance-optimized-helpers';

/**
 * E2E Test Reliability Scenarios
 *
 * Tests that validate application reliability under various conditions
 * and ensure consistent behavior in the Testing configuration.
 */

test.describe('@critical Reliability - Form Interaction Patterns', () => {
  let reliabilityHelpers: ReliabilityHelpers;

  test.beforeEach(async ({ page }) => {
    reliabilityHelpers = new ReliabilityHelpers(page);
  });

  test('@critical Reliable form submission with validation', async ({ page, baseURL, apiUrl }) => {
    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Navigate to people using reliable navigation
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');

    // Open form reliably
    await reliabilityHelpers.clickButtonSafely('button:has-text("Add New Person")', {
      expectedStateAfterClick: async () => {
        return await page.locator('app-people form').isVisible();
      }
    });

    // Fill form with reliable input handling
    const testUser = {
      fullName: `Reliable Test User ${Date.now()}`,
      phone: '+1-555-0001'
    };

    await reliabilityHelpers.fillFormFieldSafely('input#fullName', testUser.fullName, {
      validateInput: true
    });

    await reliabilityHelpers.fillFormFieldSafely('input#phone', testUser.phone, {
      validateInput: true
    });

    // Submit form with state validation
    await reliabilityHelpers.clickButtonSafely('button[type="submit"]:has-text("Save")', {
      expectedStateAfterClick: async () => {
        // Either we're back at the list or we see a success message
        return await page.locator('app-people-list').isVisible() ||
               await page.locator('.success, .alert-success').isVisible();
      }
    });

    // Validate data persistence using reliable API checking
    const perfHelpers = new PerformanceOptimizedHelpers(page, page.request, apiUrl);
    await reliabilityHelpers.validateDataConsistency(
      async () => {
        const response = await page.request.get(`${apiUrl}/api/people`);
        return await response.json();
      },
      (people) => {
        const createdPerson = people.find((p: any) => p.fullName === testUser.fullName);
        expect(createdPerson).toBeTruthy();
        expect(createdPerson.phone).toBe(testUser.phone);
      },
      { errorMessage: 'Created person not found in API response' }
    );
  });

  test('@critical Reliable form validation error handling', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Navigate and open form
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');
    await reliabilityHelpers.clickButtonSafely('button:has-text("Add New Person")');

    // Try to submit empty form
    await reliabilityHelpers.clickButtonSafely('button[type="submit"]:has-text("Save")', {
      expectedStateAfterClick: async () => {
        // Form should still be visible (validation failed)
        return await page.locator('app-people form').isVisible();
      }
    });

    // Fill with invalid data
    await reliabilityHelpers.fillFormFieldSafely('input#fullName', ''); // Empty required field
    await reliabilityHelpers.fillFormFieldSafely('input#phone', 'invalid-phone-number');

    // Submit and verify validation
    await reliabilityHelpers.clickButtonSafely('button[type="submit"]:has-text("Save")', {
      expectedStateAfterClick: async () => {
        // Form should still be visible with validation errors
        return await page.locator('app-people form').isVisible();
      }
    });

    // Fix validation errors
    await reliabilityHelpers.fillFormFieldSafely('input#fullName', 'Valid User Name');
    await reliabilityHelpers.fillFormFieldSafely('input#phone', '+1-555-0002');

    // Submit successfully
    await reliabilityHelpers.clickButtonSafely('button[type="submit"]:has-text("Save")', {
      expectedStateAfterClick: async () => {
        return await page.locator('app-people-list').isVisible();
      }
    });
  });
});

test.describe('@critical Reliability - Navigation and State Management', () => {
  let reliabilityHelpers: ReliabilityHelpers;

  test.beforeEach(async ({ page }) => {
    reliabilityHelpers = new ReliabilityHelpers(page);
  });

  test('@critical Reliable multi-module navigation', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Capture initial state
    const initialState = await reliabilityHelpers.capturePageState();

    // Navigate to People
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForNavigationToComplete(/people/);
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');

    // Navigate to Roles
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/roles-list"]', 'click');
    await reliabilityHelpers.waitForNavigationToComplete(/roles/);
    await reliabilityHelpers.waitForElementToBeReady('app-roles-list');

    // Test browser back/forward reliability
    await page.goBack();
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');

    await page.goForward();
    await reliabilityHelpers.waitForElementToBeReady('app-roles-list');

    // Return to home if possible
    const homeLink = page.locator('a[routerLink="/"], .navbar-brand');
    if (await homeLink.isVisible({ timeout: 2000 })) {
      await reliabilityHelpers.interactWithElementInContext('a[routerLink="/"], .navbar-brand', 'click');
      await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');
    }
  });

  test('@critical Reliable page refresh handling', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Navigate to a specific page
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');

    // Capture state before refresh
    const preRefreshState = await reliabilityHelpers.capturePageState();

    // Refresh page
    await page.reload();

    // Verify app recovers correctly
    await reliabilityHelpers.waitForElementToBeReady('app-people-list', {
      timeout: 15000 // Allow extra time for refresh
    });

    // Verify URL and basic functionality
    expect(page.url()).toContain('people');
    await expect(page.locator('app-people-list')).toBeVisible();
  });
});

test.describe('@extended Reliability - Error Recovery Scenarios', () => {
  let reliabilityHelpers: ReliabilityHelpers;

  test.beforeEach(async ({ page }) => {
    reliabilityHelpers = new ReliabilityHelpers(page);
  });

  test('@extended Reliable handling of network conditions', async ({ page, baseURL, apiUrl }) => {
    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test API resilience
    await reliabilityHelpers.executeWithNetworkResilience(async () => {
      const response = await page.request.get(`${apiUrl}/api/people`);
      expect(response.ok()).toBe(true);
    });

    // Test UI resilience to network delays
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForElementToBeReady('app-people-list', {
      timeout: 15000 // Extended timeout for network conditions
    });
  });

  test('@extended Reliable dialog and modal handling', async ({ page, baseURL, apiUrl }) => {
    const perfHelpers = new PerformanceOptimizedHelpers(page, page.request, apiUrl);

    // Create a test person to delete (which might show confirmation dialog)
    const testPerson = await perfHelpers.createTestPerson();

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');
    await reliabilityHelpers.interactWithElementInContext('a[routerLink="/people-list"]', 'click');
    await reliabilityHelpers.waitForElementToBeReady('app-people-list');

    // Handle potential delete confirmation dialog
    await reliabilityHelpers.handlePotentialDialogs(
      async () => {
        // Find and click delete button
        const personRow = page.locator(`tr:has-text("${testPerson.fullName}")`).first();
        if (await personRow.isVisible({ timeout: 5000 })) {
          const deleteButton = personRow.locator('button:has-text("Delete")');
          if (await deleteButton.isVisible({ timeout: 2000 })) {
            await deleteButton.click();
          }
        }
      },
      {
        confirm: async (message) => {
          console.log(`Handling confirmation dialog: ${message}`);
          return true; // Accept the deletion
        }
      }
    );

    // Verify deletion through API
    await reliabilityHelpers.validateDataConsistency(
      async () => {
        const response = await page.request.get(`${apiUrl}/api/people/${testPerson.id}`);
        return response.status();
      },
      (status) => {
        expect(status).toBe(404); // Person should be deleted
      },
      { errorMessage: 'Person was not properly deleted' }
    );
  });

  test('@extended Reliable concurrent user simulation', async ({ page, baseURL, apiUrl }) => {
    const perfHelpers = new PerformanceOptimizedHelpers(page, page.request, apiUrl);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Simulate concurrent operations
    const concurrentOperations = Array.from({ length: 3 }, async (_, i) => {
      return await reliabilityHelpers.executeWithNetworkResilience(async () => {
        const person = await perfHelpers.createTestPerson({
          fullName: `Concurrent User ${i}`,
          phone: `+1-555-00${i}0`
        });

        // Verify creation
        const response = await page.request.get(`${apiUrl}/api/people/${person.id}`);
        expect(response.ok()).toBe(true);

        // Cleanup
        await perfHelpers.cleanupTestPerson(person.id!);

        return person;
      });
    });

    const results = await Promise.all(concurrentOperations);
    expect(results).toHaveLength(3);
  });
});

test.describe('@smoke Reliability - Baseline Validation', () => {
  test('@smoke Reliable application startup detection', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);

    // Use reliable waiting for app to be ready
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")', {
      timeout: 15000
    });

    // Verify Angular is properly loaded
    const isAngularReady = await page.evaluate(() => {
      return typeof (window as any).ng !== 'undefined';
    });

    expect(isAngularReady).toBe(true);
  });

  test('@smoke Reliable API connectivity check', async ({ page, apiUrl }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    // Test API availability with retry
    await reliabilityHelpers.executeWithNetworkResilience(async () => {
      const response = await page.request.get(`${apiUrl}/health`);
      expect(response.ok()).toBe(true);
    });

    // Test API data endpoints
    const endpoints = ['/api/people', '/api/roles'];

    for (const endpoint of endpoints) {
      await reliabilityHelpers.executeWithNetworkResilience(async () => {
        const response = await page.request.get(`${apiUrl}${endpoint}`);
        expect(response.ok()).toBe(true);

        const data = await response.json();
        expect(Array.isArray(data)).toBe(true);
      });
    }
  });

  test('@smoke Reliable element interaction verification', async ({ page, baseURL }) => {
    const reliabilityHelpers = new ReliabilityHelpers(page);

    await page.goto(baseURL);
    await reliabilityHelpers.waitForElementToBeReady('h1:has-text("CRUD Template Application")');

    // Test reliable link interactions
    const links = ['a[routerLink="/people-list"]', 'a[routerLink="/roles-list"]'];

    for (const linkSelector of links) {
      // Navigate
      await reliabilityHelpers.interactWithElementInContext(linkSelector, 'click', undefined, {
        waitForStable: true
      });

      // Verify navigation
      await reliabilityHelpers.waitForNavigationToComplete();

      // Verify page loaded
      const expectedComponent = linkSelector.includes('people') ? 'app-people-list' : 'app-roles-list';
      await reliabilityHelpers.waitForElementToBeReady(expectedComponent);
    }
  });
});