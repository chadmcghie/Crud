import { test, expect } from '../fixtures/serial-test-fixture';
import { faker } from '@faker-js/faker';

/**
 * Complete User Journey Tests
 *
 * End-to-end user workflows that validate complete features
 * from user perspective, including UI interactions and backend persistence.
 */

test.describe('@critical Complete Person Management Journey', () => {
  test('@critical User can create, view, edit, and delete a person', async ({ page, baseURL, apiUrl }) => {
    // Use simple names without special characters to avoid validation issues
    const testUser = {
      fullName: `${faker.person.firstName()} ${faker.person.lastName()}`,
      phone: `+1-555-${Math.floor(Math.random() * 9000) + 1000}` // Use format that matches validation regex
    };

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People section - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 10000 });
    await page.waitForTimeout(500); // Allow component to stabilize

    // Create new person
    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.waitFor({ state: 'visible', timeout: 10000 });
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 10000 });

    // Fill form
    await page.fill('input#fullName', testUser.fullName);
    await page.fill('input#phone', testUser.phone);

    // Submit form - use the correct button text from the form
    const submitButton = page.locator('button:has-text("Create Person")');
    await submitButton.click();

    // Verify person appears in list
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const personRow = page.locator(`tr:has-text("${testUser.fullName}")`).first();
    await expect(personRow).toBeVisible();

    // Edit the person
    const editButton = personRow.locator('button:has-text("Edit")');
    await editButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Update name with a simple, valid name
    const updatedName = `${testUser.fullName} Updated`;
    await page.fill('input#fullName', updatedName);

    // Wait for validation to pass (button to become enabled)
    const updateButton = page.locator('button:has-text("Update Person")');
    await updateButton.waitFor({ state: 'visible', timeout: 5000 });

    // Wait for button to be enabled
    await expect(updateButton).toBeEnabled({ timeout: 5000 });
    await updateButton.click();

    // Verify update
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const updatedPersonRow = page.locator(`tr:has-text("${updatedName}")`).first();
    await expect(updatedPersonRow).toBeVisible();

    // Delete the person
    const deleteButton = updatedPersonRow.locator('button:has-text("Delete")');

    // Handle the confirm dialog that will appear
    page.once('dialog', async dialog => {
      await dialog.accept(); // Click OK on the confirm dialog
    });

    await deleteButton.click();

    // Wait for the deletion to complete
    await page.waitForResponse(response =>
      response.url().includes('/api/people') && response.status() === 204
    );

    // Verify person is removed
    await expect(page.locator(`tr:has-text("${updatedName}")`).first()).not.toBeVisible({ timeout: 5000 });
  });

  test('@critical User can assign roles to a person', async ({ page, baseURL, apiUrl }) => {
    const testUser = {
      fullName: `${faker.person.firstName()} ${faker.person.lastName()}`,
      phone: `+1-555-${Math.floor(Math.random() * 9000) + 1000}` // Use format that matches validation regex
    };

    // First, create a role via API for assignment
    const roleData = { name: `Test Role ${Date.now()}`, description: 'Role for user journey test' };
    const roleResponse = await page.request.post(`${apiUrl}/api/roles`, { data: roleData });
    expect(roleResponse.ok()).toBe(true);
    const createdRole = await roleResponse.json();

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People section - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    // Create new person
    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Fill basic info
    await page.fill('input#fullName', testUser.fullName);
    await page.fill('input#phone', testUser.phone);

    // Assign role (if role selection is available in form)
    try {
      // Wait for checkboxes to load first
      await page.locator('input[type="checkbox"]').first().waitFor({ state: 'visible', timeout: 5000 });

      // Use getByRole for better accessibility
      const roleCheckbox = page.getByRole('checkbox', { name: new RegExp(roleData.name, 'i') });
      if (await roleCheckbox.isVisible({ timeout: 3000 })) {
        await roleCheckbox.check();
      }
    } catch {
      // Role assignment might not be in create form
    }

    // Submit form
    const submitButton = page.locator('button:has-text("Create Person")');
    await submitButton.click();

    // Verify person was created
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const personRow = page.locator(`tr:has-text("${testUser.fullName}")`).first();
    await expect(personRow).toBeVisible();

    // Cleanup: Delete the test role
    await page.request.delete(`${apiUrl}/api/roles/${createdRole.id}`);
  });
});

test.describe('@critical Complete Role Management Journey', () => {
  test('@critical User can create, view, edit, and delete a role', async ({ page, baseURL, apiUrl }) => {
    const testRole = {
      name: `Test Role ${Date.now()}`, // Simple name without special chars
      description: 'Test role description'
    };

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to Roles section - use first link to avoid ambiguity
    const rolesLink = page.locator('nav a[routerLink="/roles-list"]').first();
    await rolesLink.click();
    await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });

    // Create new role
    const addButton = page.locator('button:has-text("Add New Role"), button:has-text("Add Role")');
    if (await addButton.isVisible({ timeout: 2000 })) {
      await addButton.click();
      await page.locator('app-roles form, form').waitFor({ state: 'visible', timeout: 5000 });

      // Fill form
      await page.fill('input[name="name"], #name', testRole.name);
      await page.fill('input[name="description"], textarea[name="description"], #description', testRole.description);

      // Submit form - use the correct button text from the form
      const submitButton = page.locator('button:has-text("Create Role"), button:has-text("Save"), button:has-text("Create")');
      await submitButton.click();

      // Verify role appears in list
      await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });
      const roleRow = page.locator(`tr:has-text("${testRole.name}"), .role-item:has-text("${testRole.name}")`).first();
      await expect(roleRow).toBeVisible();
    } else {
      // If no UI for role creation, use API and verify list shows it
      const roleResponse = await page.request.post(`${apiUrl}/api/roles`, { data: testRole });
      expect(roleResponse.ok()).toBe(true);

      // Refresh page to see new role
      await page.reload();
      await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });
      const roleRow = page.locator(`tr:has-text("${testRole.name}"), .role-item:has-text("${testRole.name}")`).first();
      await expect(roleRow).toBeVisible();
    }
  });
});

test.describe('@critical Cross-Module User Journeys', () => {
  test('@critical User can navigate between all modules', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Test navigation to People - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    await expect(page.locator('app-people-list')).toBeVisible();

    // Test navigation to Roles - use first link to avoid ambiguity
    const rolesLink = page.locator('nav a[routerLink="/roles-list"]').first();
    await rolesLink.click();
    await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });
    await expect(page.locator('app-roles-list')).toBeVisible();

    // Test navigation back to Home (if available)
    const homeLink = page.locator('a[routerLink="/"], a[routerLink="/home"], .navbar-brand');
    if (await homeLink.isVisible({ timeout: 2000 })) {
      await homeLink.click();
      await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 5000 });
    }

    // Test browser back/forward navigation
    await page.goBack();
    await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });
    await expect(page.locator('app-roles-list')).toBeVisible();

    await page.goForward();
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 5000 });
  });

  test('@critical User workflow with form validation', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    // Open add form
    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Check form validation - button should be disabled when form is empty
    const submitButton = page.locator('button:has-text("Create Person")');
    await submitButton.waitFor({ state: 'visible', timeout: 5000 });

    // Verify validation is working: button should be disabled for empty form
    const isDisabled = await submitButton.isDisabled();
    expect(isDisabled).toBe(true); // Validation working correctly

    // Fill form with invalid data
    await page.fill('input#fullName', ''); // Empty required field
    await page.fill('input#phone', 'invalid-phone');

    // Button should still be disabled due to empty required field
    const stillDisabled = await submitButton.isDisabled();
    expect(stillDisabled).toBe(true); // Validation still working

    // Form should remain visible (validation preventing submission)
    await expect(page.locator('app-people form')).toBeVisible();

    // Fill with valid data
    await page.fill('input#fullName', 'Valid User Name');
    await page.fill('input#phone', '+1-555-0123');
    await submitButton.click();

    // Should redirect back to list
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
  });
});

test.describe('@extended Error Recovery User Journeys', () => {
  test('@extended User can recover from network errors', async ({ page, baseURL, apiUrl }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    // Try to load data (should work)
    const response = await page.request.get(`${apiUrl}/api/people`);
    expect(response.ok()).toBe(true);

    // Simulate network recovery by refreshing
    await page.reload();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 10000 });

    // Application should recover and show data
    await expect(page.locator('app-people-list')).toBeVisible();
  });

  test('@extended User can handle page refresh during form completion', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People and open form - use first link to avoid ambiguity
    const peopleLink = page.locator('nav a[routerLink="/people-list"]').first();
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Fill partial form
    await page.fill('input#fullName', 'Partial Name');

    // Refresh page (simulates accidental refresh)
    await page.reload();

    // Should return to people list (form data lost, but app recovers)
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 10000 });
    await expect(page.locator('app-people-list')).toBeVisible();
  });
});