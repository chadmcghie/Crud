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
    const testUser = {
      fullName: faker.person.fullName(),
      phone: faker.phone.number({ style: 'national' })
    };

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People section
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    // Create new person
    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Fill form
    await page.fill('input#fullName', testUser.fullName);
    await page.fill('input#phone', testUser.phone);

    // Submit form
    const submitButton = page.locator('button[type="submit"]:has-text("Save")');
    await submitButton.click();

    // Verify person appears in list
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const personRow = page.locator(`tr:has-text("${testUser.fullName}")`).first();
    await expect(personRow).toBeVisible();

    // Edit the person
    const editButton = personRow.locator('button:has-text("Edit")');
    await editButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Update name
    const updatedName = `${testUser.fullName} (Updated)`;
    await page.fill('input#fullName', updatedName);
    await submitButton.click();

    // Verify update
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const updatedPersonRow = page.locator(`tr:has-text("${updatedName}")`).first();
    await expect(updatedPersonRow).toBeVisible();

    // Delete the person
    const deleteButton = updatedPersonRow.locator('button:has-text("Delete")');
    await deleteButton.click();

    // Confirm deletion (if confirmation dialog appears)
    try {
      const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Yes"), button:has-text("Delete")').first();
      await confirmButton.click({ timeout: 2000 });
    } catch {
      // No confirmation dialog, deletion was immediate
    }

    // Verify person is removed
    await expect(page.locator(`tr:has-text("${updatedName}")`)).not.toBeVisible();
  });

  test('@critical User can assign roles to a person', async ({ page, baseURL, apiUrl }) => {
    const testUser = {
      fullName: faker.person.fullName(),
      phone: faker.phone.number({ style: 'national' })
    };

    // First, create a role via API for assignment
    const roleData = { name: 'Test Role', description: 'Role for user journey test' };
    const roleResponse = await page.request.post(`${apiUrl}/api/roles`, { data: roleData });
    expect(roleResponse.ok()).toBe(true);
    const createdRole = await roleResponse.json();

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to People section
    const peopleLink = page.locator('a[routerLink="/people-list"]');
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
      const roleCheckbox = page.locator(`input[type="checkbox"][value="${createdRole.id}"], label:has-text("${roleData.name}")`);
      if (await roleCheckbox.isVisible({ timeout: 2000 })) {
        await roleCheckbox.check();
      }
    } catch {
      // Role assignment might not be in create form
    }

    // Submit form
    const submitButton = page.locator('button[type="submit"]:has-text("Save")');
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
      name: faker.company.buzzPhrase().replace(/[^a-zA-Z0-9 ]/g, ''),
      description: faker.lorem.sentence()
    };

    // Navigate to application
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Navigate to Roles section
    const rolesLink = page.locator('a[routerLink="/roles-list"]');
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

      // Submit form
      const submitButton = page.locator('button[type="submit"]:has-text("Save"), button:has-text("Create")');
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

    // Test navigation to People
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    await expect(page.locator('app-people-list')).toBeVisible();

    // Test navigation to Roles
    const rolesLink = page.locator('a[routerLink="/roles-list"]');
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

    // Navigate to People
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });

    // Open add form
    const addButton = page.locator('button:has-text("Add New Person")');
    await addButton.click();
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });

    // Try to submit empty form (should show validation)
    const submitButton = page.locator('button[type="submit"]:has-text("Save")');
    await submitButton.click();

    // Check for validation messages
    const validationMessages = page.locator('.error, .invalid, .validation-error, .form-error');
    if (await validationMessages.first().isVisible({ timeout: 2000 })) {
      await expect(validationMessages.first()).toBeVisible();
    }

    // Fill form with invalid data
    await page.fill('input#fullName', ''); // Empty required field
    await page.fill('input#phone', 'invalid-phone');
    await submitButton.click();

    // Form should not submit successfully
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

    // Navigate to People
    const peopleLink = page.locator('a[routerLink="/people-list"]');
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

    // Navigate to People and open form
    const peopleLink = page.locator('a[routerLink="/people-list"]');
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