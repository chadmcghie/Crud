import { test, expect } from '@playwright/test';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestPerson, generateTestRole, testPeople } from '../helpers/test-data';

test.describe('People Management UI', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, request }) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(request);
    
    // Clean up any existing data
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll();
        // Wait a bit for cleanup to complete
        await page.waitForTimeout(500);
      } catch (error) {
        console.warn('Failed to cleanup before test:', error);
      }
    }
    
    // Navigate to the app (people tab is default)
    await pageHelpers.navigateToApp();
    await pageHelpers.switchToPeopleTab();
    
    // Wait for the page to fully load
    await page.waitForTimeout(1000);
  });

  test.afterEach(async () => {
    // Clean up after each test
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll();
      } catch (error) {
        console.warn('Failed to cleanup after test:', error);
      }
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
    await apiHelpers.createPerson(testPerson);
    
    // Refresh the page to see the new person
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    // Verify person exists before deletion
    await pageHelpers.verifyPersonExists(testPerson.fullName);
    
    // Delete the person
    await pageHelpers.deletePerson(testPerson.fullName);
    
    // Verify person no longer exists
    await pageHelpers.verifyPersonNotExists(testPerson.fullName);
    
    // Verify empty state is shown
    await pageHelpers.verifyEmptyState('people');
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
    await apiHelpers.createPerson(testPerson);
    
    await pageHelpers.refreshPage();
    await pageHelpers.switchToPeopleTab();
    
    // Edit person to add roles
    await pageHelpers.editPerson(testPerson.fullName);
    
    // Check role checkboxes
    await page.check(`input[type="checkbox"]:near(label:has-text("${role1.name}"))`);
    await page.check(`input[type="checkbox"]:near(label:has-text("${role2.name}"))`);
    
    await pageHelpers.updatePersonForm();
    
    // Verify roles are assigned
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, role1.name);
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, role2.name);
    
    // Edit again to remove one role
    await pageHelpers.editPerson(testPerson.fullName);
    await page.uncheck(`input[type="checkbox"]:near(label:has-text("${role1.name}"))`);
    await pageHelpers.updatePersonForm();
    
    // Verify only one role remains
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, role2.name);
    
    const personRow = page.locator(`tr:has-text("${testPerson.fullName}")`);
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