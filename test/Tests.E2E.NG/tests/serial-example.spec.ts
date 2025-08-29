import { test, expect, tagTest, helpers } from './fixtures/serial-test-fixture';

/**
 * Example test file demonstrating serial test execution with proper tagging
 * This shows the patterns to follow for all test files
 */

test.describe('People Management - Serial Tests', () => {
  let apiUrl: string;
  
  test.beforeAll(async () => {
    // Shared setup that runs once for all tests in this file
    apiUrl = process.env.API_URL || 'http://localhost:5172';
    console.log(`📍 Running tests against API: ${apiUrl}`);
  });
  
  // Smoke tests - quick validation of core functionality (2 min total)
  test(tagTest('should load the people list page', 'smoke'), async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 10000 });
    
    // Click People tab
    const peopleTab = page.locator('button:has-text("👥 People Management")');
    await peopleTab.click();
    await page.waitForSelector('app-people-list', { timeout: 5000 });
    
    await expect(page.locator('h3')).toContainText(/People Directory/i);
  });
  
  test(tagTest('should display the add person button', 'smoke'), async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 10000 });
    
    // Click People tab
    const peopleTab = page.locator('button:has-text("👥 People Management")');
    await peopleTab.click();
    await page.waitForSelector('app-people-list', { timeout: 5000 });
    
    const addButton = page.locator('button:has-text("Add New Person")');
    await expect(addButton).toBeVisible();
  });
  
  // Critical tests - essential user workflows (5 min total)
  test(tagTest('should create a new person through UI', 'critical'), async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 10000 });
    
    // Click People tab
    const peopleTab = page.locator('button:has-text("👥 People Management")');
    await peopleTab.click();
    await page.waitForSelector('app-people-list', { timeout: 5000 });
    
    // Click add button
    await page.click('button:has-text("Add New Person")');
    await page.waitForSelector('app-people form', { timeout: 5000 });
    
    // Fill in the form with valid name format
    const testName = `John Test Smith`;
    
    await page.fill('input#fullName', testName);
    await page.fill('input#phone', '+1-555-0123');
    
    // Submit the form
    await page.click('button[type="submit"]:has-text("Create Person")');
    
    // Wait for form to close or person to appear in list
    await page.waitForSelector('app-people form', { state: 'hidden', timeout: 10000 }).catch(() => {
      // Form might stay open, check if person was added
    });
    
    // Verify the person appears in the list
    await page.goto(`${baseURL}`);
    await expect(page.locator(`text=${testName}`)).toBeVisible({ timeout: 10000 });
  });
  
  test(tagTest('should edit an existing person', 'critical'), async ({ page, baseURL, apiUrl }) => {
    // Create test data via API for consistent state with unique name
    const uniqueLetter = String.fromCharCode(65 + Math.floor(Math.random() * 26)); // Random A-Z
    const testPerson = await helpers.createTestData(page, apiUrl, 'api/people', {
      fullName: `Edit Test Person ${uniqueLetter}`,
      phone: '555-0100'
    });
    
    // Navigate to people list
    await page.goto(`${baseURL}`);
    
    // Find and click edit for the test person
    const row = page.locator(`tr:has-text("${testPerson.fullName}")`);
    await row.locator('button:has-text("Edit")').click();
    
    // Wait for the edit form to appear
    await page.waitForSelector('.people-form-container', { timeout: 5000 });
    
    // Update the name
    const updatedName = `${testPerson.fullName} Updated`;
    await page.fill('input#fullName', updatedName);
    
    // Save changes - look for Update Person button
    await page.click('button[type="submit"]:has-text("Update Person")');
    
    // Verify the update
    await page.goto(`${baseURL}`);
    await expect(page.locator(`text=${updatedName}`)).toBeVisible();
    
    // Cleanup
    await helpers.cleanupTestData(page, apiUrl, 'api/people', testPerson.id);
  });
  
  test(tagTest('should delete a person', 'critical'), async ({ page, baseURL, apiUrl }) => {
    // Create test data with unique name using letters
    const uniqueLetter = String.fromCharCode(65 + Math.floor(Math.random() * 26)); // Random A-Z
    const testPerson = await helpers.createTestData(page, apiUrl, 'api/people', {
      fullName: `Delete Test Person ${uniqueLetter}`,
      phone: '555-0200'
    });
    
    // Navigate to people list
    await page.goto(`${baseURL}`);
    
    // Find and click delete for the test person
    const row = page.locator(`tr:has-text("${testPerson.fullName}")`);
    
    // Handle the confirm dialog that will appear
    page.once('dialog', async dialog => {
      await dialog.accept(); // Click OK on the confirm dialog
    });
    
    await row.locator('button:has-text("Delete")').click();
    
    // Wait for the deletion to complete
    await page.waitForResponse(response => 
      response.url().includes('/api/people') && response.status() === 204
    );
    
    // Verify the person is deleted - use first() to avoid multiple matches
    await expect(page.locator(`td:has-text("${testPerson.fullName}")`).first()).not.toBeVisible({ timeout: 5000 });
  });
  
  // Extended tests - comprehensive scenarios (10 min total)
  test(tagTest('should handle validation errors when creating person', 'extended'), async ({ page, baseURL }) => {
    await page.goto(`${baseURL}`);
    
    // Click People tab first
    const peopleTab = page.locator('button:has-text("👥 People Management")');
    await peopleTab.click();
    await page.waitForSelector('app-people-list', { timeout: 5000 });
    
    // Click add button - be more specific with the selector
    await page.click('button:has-text("Add New Person")');
    
    // Wait for form to appear
    await page.waitForSelector('.people-form-container, app-people form', { timeout: 5000 });
    
    // Fill in only partial data to trigger validation
    await page.fill('input#fullName', 'Test');
    await page.fill('input#fullName', ''); // Clear to trigger required validation
    
    // Try to submit form - the button should be disabled due to validation
    const submitButton = page.locator('button[type="submit"]:has-text("Create Person")');
    
    // Verify submit button is disabled when form is invalid
    await expect(submitButton).toBeDisabled();
    
    // Check for validation errors on the field
    const fullNameInput = page.locator('input#fullName');
    await fullNameInput.blur(); // Trigger validation
    
    // Look for validation error message
    const errorMessage = page.locator('.invalid-feedback, .error-message, .text-danger').first();
    const hasError = await errorMessage.isVisible({ timeout: 2000 }).catch(() => false);
    
    if (hasError) {
      await expect(errorMessage).toContainText(/required|必須/i);
    } else {
      // If no error message, at least verify button stays disabled
      await expect(submitButton).toBeDisabled();
    }
  });
  
  test(tagTest('should filter people list by search term', 'extended'), async ({ page, baseURL, apiUrl }) => {
    // Create multiple test people
    const people = await Promise.all([
      helpers.createTestData(page, apiUrl, 'api/people', {
        fullName: 'Alice Searchtest',
        phone: '555-0301'
      }),
      helpers.createTestData(page, apiUrl, 'api/people', {
        fullName: 'Bob Searchtest',
        phone: '555-0302'
      }),
      helpers.createTestData(page, apiUrl, 'api/people', {
        fullName: 'Charlie Different',
        phone: '555-0303'
      })
    ]);
    
    // Navigate to people list
    await page.goto(`${baseURL}`);
    
    // Search for "Searchtest"
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i]').first();
    if (await searchInput.isVisible({ timeout: 2000 })) {
      await searchInput.fill('Searchtest');
      // Wait for search results to update (watch for network activity or DOM changes)
      await page.waitForResponse(response => 
        response.url().includes('/api/people') && response.ok(),
        { timeout: 5000 }
      ).catch(() => {
        // If no API call, wait for DOM to update
        return page.waitForFunction(
          (searchTerm) => document.body.textContent?.includes(searchTerm),
          'Alice Searchtest',
          { timeout: 2000 }
        );
      });
      
      // Verify filtered results
      await expect(page.locator('text=Alice Searchtest')).toBeVisible();
      await expect(page.locator('text=Bob Searchtest')).toBeVisible();
      await expect(page.locator('text=Charlie Different')).not.toBeVisible();
    }
    
    // Cleanup
    for (const person of people) {
      await helpers.cleanupTestData(page, apiUrl, 'api/people', person.id);
    }
  });
  
  test(tagTest('should paginate through people list', 'extended'), async ({ page, baseURL, apiUrl }) => {
    // Create enough people to trigger pagination (assuming 10 per page)
    const people = [];
    for (let i = 1; i <= 15; i++) {
      people.push(await helpers.createTestData(page, apiUrl, 'api/people', {
        fullName: `Pagination Test Person ${String.fromCharCode(65 + i - 1)}`,  // A, B, C, etc.
        phone: `555-04${i.toString().padStart(2, '0')}`
      }));
    }
    
    // Navigate to people list
    await page.goto(`${baseURL}`);
    
    // Check if pagination controls exist
    const nextButton = page.locator('button:has-text("Next"), a:has-text("Next"), [aria-label="Next"]').first();
    if (await nextButton.isVisible({ timeout: 2000 })) {
      // Go to next page
      await nextButton.click();
      // Wait for page content to update
      await page.waitForFunction(
        () => {
          // Check if the DOM has updated with new content
          const rows = document.querySelectorAll('tbody tr');
          return rows.length > 0;
        },
        { timeout: 5000 }
      );
      
      // Verify we're on page 2 (some items from second batch should be visible)
      const page2Item = page.locator('text=Pagination Test Person K').first();
      await expect(page2Item).toBeVisible();
    }
    
    // Cleanup
    for (const person of people) {
      await helpers.cleanupTestData(page, apiUrl, 'api/people', person.id);
    }
  });
});

// API-only tests (no UI interaction)
test.describe('People API - Serial Tests', () => {
  test(tagTest('should handle concurrent API requests', 'extended'), async ({ page, apiUrl }) => {
    // Create multiple people concurrently
    const promises = [];
    for (let i = 0; i < 5; i++) {
      promises.push(
        page.request.post(`${apiUrl}/api/people`, {
          data: {
            fullName: `Concurrent Test Person ${String.fromCharCode(65 + i)}`,  // A, B, C, etc.
            phone: `555-05${i}0`
          }
        })
      );
    }
    
    const responses = await Promise.all(promises);
    
    // All should succeed
    for (const response of responses) {
      expect(response.ok()).toBe(true);
    }
    
    // Cleanup
    for (const response of responses) {
      const person = await response.json();
      await page.request.delete(`${apiUrl}/api/people/${person.id}`);
    }
  });
});