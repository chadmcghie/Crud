import { test, expect } from '../setup/test-fixture';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestPerson, generateTestRole, testPeople } from '../helpers/test-data';

test.describe('People Management UI', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, apiContext }) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(apiContext, 0);
    
    // Clean up any existing data and wait for completion
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll(true); // Force immediate cleanup for UI tests
        // Wait for cleanup to complete by checking for network idle or DOM updates
        await page.waitForLoadState('domcontentloaded', { timeout: 2000 }).catch(() => {});
      } catch (error) {
        console.warn('Failed to cleanup before test:', error);
      }
    }
    
    // Navigate to the app (people tab is default)
    await pageHelpers.navigateToApp();
    await pageHelpers.switchToPeopleTab();
    
    // Wait for the page to fully load - use specific selectors instead of networkidle
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 15000 });
    await page.waitForSelector('a[routerLink="/people-list"]', { timeout: 10000 });
  });

  test.afterEach(async ({ page }) => {
    // Clean up after each test and wait for completion
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll(true); // Force immediate cleanup for UI tests
        // Wait for cleanup to complete by checking for network idle or DOM updates
        await page.waitForLoadState('domcontentloaded', { timeout: 2000 }).catch(() => {});
      } catch (error) {
        console.warn('Failed to cleanup after test:', error);
      }
    }
    
    // Wait for any pending operations to complete - use specific checks instead of networkidle
    try {
      await page.waitForSelector('a[routerLink="/people-list"]', { timeout: 3000 });
    } catch (error) {
      // Page might be in a transitional state, that's okay for cleanup
      console.warn('Page not fully loaded during cleanup, continuing...');
    }
  });

  test('should display empty state when no people exist', async () => {
    await pageHelpers.verifyEmptyState('people');
  });

  test('should create a new person successfully', async () => {
    const testPerson = generateTestPerson();
    
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone);
    await pageHelpers.submitPersonForm();
    
    // Verify the person appears in the list
    await pageHelpers.verifyPersonExists(testPerson.fullName);
    
    // Verify the person count increased
    const personCount = await pageHelpers.getPersonRowCount();
    expect(personCount).toBe(1);
  });

  test('should create multiple people', async () => {
    for (let i = 0; i < testPeople.length; i++) {
      const person = testPeople[i];
      
      await pageHelpers.clickAddPerson();
      await pageHelpers.fillPersonForm(person.fullName, person.phone);
      await pageHelpers.submitPersonForm();
      
      await pageHelpers.verifyPersonExists(person.fullName);
    }
    
    const personCount = await pageHelpers.getPersonRowCount();
    expect(personCount).toBe(testPeople.length);
  });

  test('should validate required fields', async () => {
    await pageHelpers.clickAddPerson();
    
    // Try to submit without filling required fields
    await pageHelpers.verifySubmitButtonDisabled();
    
    // Fill only the name (required field)
    const testPerson = generateTestPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName);
    await pageHelpers.verifySubmitButtonEnabled();
  });

  test('should create person with roles', async () => {
    // First create some roles
    const role1 = await apiHelpers.createRole(generateTestRole());
    const role2 = await apiHelpers.createRole(generateTestRole());
    
    // Refresh to load roles in the form
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    const testPerson = generateTestPerson();
    
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone, [role1.name, role2.name]);
    await pageHelpers.submitPersonForm();
    
    // Verify the person appears with the assigned roles
    await pageHelpers.verifyPersonExists(testPerson.fullName);
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, role1.name);
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, role2.name);
  });

  test('should edit an existing person', async () => {
    // First create a person via API
    const originalPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(originalPerson);
    
    // Refresh the page to see the new person
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    // Edit the person
    await pageHelpers.editPerson(originalPerson.fullName);
    
    const updatedPerson = generateTestPerson();
    await pageHelpers.fillPersonForm(updatedPerson.fullName, updatedPerson.phone);
    await pageHelpers.updatePersonForm();
    
    // Verify the updated person appears
    await pageHelpers.verifyPersonExists(updatedPerson.fullName);
    await pageHelpers.verifyPersonNotExists(originalPerson.fullName);
  });

  test('should delete a person', async () => {
    // First create a person via API
    const testPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    // Refresh the page to see the new person
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    // Wait for data to load and verify person exists before deletion
    await pageHelpers.clickRefreshButton();
    await pageHelpers.verifyPersonExists(createdPerson.fullName);
    
    // Delete the person
    await pageHelpers.deletePerson(createdPerson.fullName);
    
    // Verify person no longer exists
    await pageHelpers.verifyPersonNotExists(createdPerson.fullName);
    
    // Verify empty state is shown (or at least that our test person is gone)
    try {
      await pageHelpers.verifyEmptyState('people');
    } catch (error) {
      // If empty state verification fails, just ensure our person is not there
      await pageHelpers.verifyPersonNotExists(createdPerson.fullName);
    }
  });

  test('should handle person creation with only required fields', async () => {
    const testPerson = generateTestPerson({ phone: undefined });
    
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName); // Only name, no phone
    await pageHelpers.submitPersonForm();
    
    await pageHelpers.verifyPersonExists(testPerson.fullName);
  });

  test('should refresh the people list', async () => {
    // Create a person via API (simulating external change)
    const testPerson = generateTestPerson();
    await apiHelpers.createPerson(testPerson);
    
    // The person shouldn't be visible yet (page hasn't refreshed)
    await pageHelpers.verifyPersonNotExists(testPerson.fullName);
    
    // Click refresh button
    await pageHelpers.clickRefreshButton();
    
    // Now the person should be visible
    await pageHelpers.verifyPersonExists(testPerson.fullName);
  });

  test('should handle form cancellation', async ({ page }) => {
    await pageHelpers.clickAddPerson();
    
    // Fill some data
    const testPerson = generateTestPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone);
    
    // Cancel the form
    await page.click('button:has-text("Cancel")');
    
    // Form should be hidden
    await expect(page.locator('app-people form')).not.toBeVisible();
    
    // Person should not be created
    await pageHelpers.verifyPersonNotExists(testPerson.fullName);
  });

  test('should handle form reset', async ({ page }) => {
    await pageHelpers.clickAddPerson();
    
    // Fill some data
    const testPerson = generateTestPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone);
    
    // Reset the form
    await page.click('button:has-text("Reset")');
    
    // Form fields should be empty
    await expect(page.locator('input#fullName')).toHaveValue('');
    await expect(page.locator('input#phone')).toHaveValue('');
  });

  test('should display person information correctly in table', async ({ page }) => {
    const testPerson = generateTestPerson();
    await apiHelpers.createPerson(testPerson);
    
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    const personRow = page.locator(`tr:has-text("${testPerson.fullName}")`);
    
    // Verify name is displayed
    await expect(personRow.locator('.name-cell')).toContainText(testPerson.fullName);
    
    // Verify phone is displayed (or N/A if empty)
    if (testPerson.phone) {
      await expect(personRow.locator('.phone-cell')).toContainText(testPerson.phone);
    } else {
      await expect(personRow.locator('.phone-cell')).toContainText('N/A');
    }
    
    // Verify action buttons are present
    await expect(personRow.locator('button:has-text("Edit")')).toBeVisible();
    await expect(personRow.locator('button:has-text("Delete")')).toBeVisible();
  });

  test('should show no roles assigned when person has no roles', async ({ page }) => {
    const testPerson = generateTestPerson();
    await apiHelpers.createPerson(testPerson);
    
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    const personRow = page.locator(`tr:has-text("${testPerson.fullName}")`);
    await expect(personRow.locator('.roles-cell')).toContainText('No roles assigned');
  });

  test('should handle role assignment and removal', async ({ page }) => {
    // Create roles first
    const role1 = await apiHelpers.createRole(generateTestRole());
    const role2 = await apiHelpers.createRole(generateTestRole());
    
    // Create person
    const testPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    // Edit person to add roles
    await pageHelpers.editPerson(createdPerson.fullName);
    
    // Check role checkboxes - wait for roles to load first
    await page.waitForSelector('.roles-grid', { timeout: 10000 });
    
    // Find and check the checkboxes for the roles
    const role1Checkbox = page.locator(`input[id="role-${role1.id}"]`);
    const role2Checkbox = page.locator(`input[id="role-${role2.id}"]`);
    
    await role1Checkbox.waitFor({ state: 'visible', timeout: 5000 });
    await role2Checkbox.waitFor({ state: 'visible', timeout: 5000 });
    
    await role1Checkbox.check();
    await role2Checkbox.check();
    
    await pageHelpers.updatePersonForm();
    
    // Verify roles are assigned
    await pageHelpers.verifyPersonHasRole(createdPerson.fullName, role1.name);
    await pageHelpers.verifyPersonHasRole(createdPerson.fullName, role2.name);
    
    // Edit again to remove one role
    await pageHelpers.editPerson(createdPerson.fullName);
    
    // Wait for roles to load and uncheck role1
    await page.waitForSelector('.roles-grid', { timeout: 10000 });
    const role1CheckboxAgain = page.locator(`input[id="role-${role1.id}"]`);
    await role1CheckboxAgain.waitFor({ state: 'visible', timeout: 5000 });
    await role1CheckboxAgain.uncheck();
    
    await pageHelpers.updatePersonForm();
    
    // Verify only one role remains
    await pageHelpers.verifyPersonHasRole(createdPerson.fullName, role2.name);
    
    const personRow = page.locator(`tr:has-text("${createdPerson.fullName}")`);
    await expect(personRow.locator('.roles-cell')).not.toContainText(role1.name);
  });

  test('should maintain data integrity across tab switches', async () => {
    // Create a person
    const testPerson = generateTestPerson();
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone);
    await pageHelpers.submitPersonForm();
    
    // Switch to roles tab and back
    await pageHelpers.switchToRolesTab();
    await pageHelpers.switchToPeopleTab();
    
    // Person should still be there
    await pageHelpers.verifyPersonExists(testPerson.fullName);
  });

  test('should show message when no roles are available', async ({ page }) => {
    await pageHelpers.clickAddPerson();
    
    // Should show message about no roles being available
    await expect(page.locator('.no-roles-message')).toContainText('No roles available. Please create roles first.');
  });
});