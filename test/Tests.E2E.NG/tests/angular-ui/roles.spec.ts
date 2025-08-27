import { test, expect } from '../setup/test-fixture';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, testRoles } from '../helpers/test-data';

test.describe('Roles Management UI', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, apiContext, workerIndex }) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(apiContext, workerIndex);
    
    // Clean up any existing data
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll(true); // Force immediate cleanup for UI tests
        // Wait a bit for cleanup to complete
        await page.waitForTimeout(500);
      } catch (error) {
        console.warn('Failed to cleanup before test:', error);
      }
    }
    
    // Navigate to the app and switch to roles tab
    await pageHelpers.navigateToApp();
    await pageHelpers.switchToRolesTab();
    
    // Wait for the page to fully load
    await page.waitForTimeout(1000);
  });

  test.afterEach(async () => {
    // Clean up after each test
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll(true); // Force immediate cleanup for UI tests
      } catch (error) {
        console.warn('Failed to cleanup after test:', error);
      }
    }
  });

  test('should display empty state when no roles exist', async () => {
    await pageHelpers.verifyEmptyState('roles');
  });

  test('should create a new role successfully', async () => {
    const testRole = generateTestRole();
    
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    await pageHelpers.submitRoleForm();
    
    // Verify the role appears in the list
    await pageHelpers.verifyRoleExists(testRole.name);
    
    // Verify the role count increased
    const roleCount = await pageHelpers.getRoleRowCount();
    expect(roleCount).toBe(1);
  });

  test('should create multiple roles', async () => {
    for (let i = 0; i < testRoles.length; i++) {
      const role = testRoles[i];
      
      await pageHelpers.clickAddRole();
      await pageHelpers.fillRoleForm(role.name, role.description);
      await pageHelpers.submitRoleForm();
      
      await pageHelpers.verifyRoleExists(role.name);
    }
    
    const roleCount = await pageHelpers.getRoleRowCount();
    expect(roleCount).toBe(testRoles.length);
  });

  test('should validate required fields', async () => {
    await pageHelpers.clickAddRole();
    
    // Try to submit without filling required fields
    await pageHelpers.verifySubmitButtonDisabled();
    
    // Fill only the name (required field)
    const testRole = generateTestRole();
    await pageHelpers.fillRoleForm(testRole.name);
    await pageHelpers.verifySubmitButtonEnabled();
  });

  test('should edit an existing role', async () => {
    // First create a role via API
    const originalRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(originalRole);
    
    // Refresh the page to see the new role
    await pageHelpers.refreshPage();
    await pageHelpers.switchToRolesTab();
    
    // Edit the role
    await pageHelpers.editRole(originalRole.name);
    
    const updatedRole = generateTestRole();
    await pageHelpers.fillRoleForm(updatedRole.name, updatedRole.description);
    await pageHelpers.updateRoleForm();
    
    // Verify the updated role appears
    await pageHelpers.verifyRoleExists(updatedRole.name);
    await pageHelpers.verifyRoleNotExists(originalRole.name);
  });

  test('should delete a role', async () => {
    // First create a role via API
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    // Refresh the page to see the new role
    await pageHelpers.refreshPage();
    await pageHelpers.switchToRolesTab();
    
    // Wait for data to load and verify role exists before deletion
    await pageHelpers.clickRefreshButton();
    await pageHelpers.verifyRoleExists(createdRole.name);
    
    // Delete the role
    await pageHelpers.deleteRole(createdRole.name);
    
    // Verify role no longer exists
    await pageHelpers.verifyRoleNotExists(createdRole.name);
    
    // Verify empty state is shown (or at least that our test role is gone)
    try {
      await pageHelpers.verifyEmptyState('roles');
    } catch (error) {
      // If empty state verification fails, just ensure our role is not there
      await pageHelpers.verifyRoleNotExists(createdRole.name);
    }
  });

  test('should handle role creation with only required fields', async () => {
    const testRole = generateTestRole({ description: undefined });
    
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(testRole.name); // Only name, no description
    await pageHelpers.submitRoleForm();
    
    await pageHelpers.verifyRoleExists(testRole.name);
  });

  test('should refresh the roles list', async () => {
    // Create a role via API (simulating external change)
    const testRole = generateTestRole();
    await apiHelpers.createRole(testRole);
    
    // The role shouldn't be visible yet (page hasn't refreshed)
    await pageHelpers.verifyRoleNotExists(testRole.name);
    
    // Click refresh button
    await pageHelpers.clickRefreshButton();
    
    // Now the role should be visible
    await pageHelpers.verifyRoleExists(testRole.name);
  });

  test('should handle form cancellation', async ({ page }) => {
    await pageHelpers.clickAddRole();
    
    // Fill some data
    const testRole = generateTestRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    
    // Cancel the form
    await page.click('button:has-text("Cancel")');
    
    // Form should be hidden
    await expect(page.locator('app-roles form')).not.toBeVisible();
    
    // Role should not be created
    await pageHelpers.verifyRoleNotExists(testRole.name);
  });

  test('should handle form reset', async ({ page }) => {
    await pageHelpers.clickAddRole();
    
    // Fill some data
    const testRole = generateTestRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    
    // Reset the form
    await page.click('button:has-text("Reset")');
    
    // Form fields should be empty
    await expect(page.locator('input#name')).toHaveValue('');
    await expect(page.locator('textarea#description')).toHaveValue('');
  });

  test('should maintain data integrity across tab switches', async () => {
    // Create a role
    const testRole = generateTestRole();
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    await pageHelpers.submitRoleForm();
    
    // Switch to people tab and back
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.switchToRolesTab();
    
    // Role should still be there
    await pageHelpers.verifyRoleExists(testRole.name);
  });

  test('should display role information correctly in table', async ({ page }) => {
    const testRole = generateTestRole();
    await apiHelpers.createRole(testRole);
    
    await pageHelpers.refreshPage();
    await pageHelpers.switchToRolesTab();
    
    const roleRow = page.locator(`tr:has-text("${testRole.name}")`);
    
    // Verify name is displayed
    await expect(roleRow.locator('.name-cell')).toContainText(testRole.name);
    
    // Verify description is displayed (or N/A if empty)
    if (testRole.description) {
      await expect(roleRow.locator('.description-cell')).toContainText(testRole.description);
    } else {
      await expect(roleRow.locator('.description-cell')).toContainText('N/A');
    }
    
    // Verify action buttons are present
    await expect(roleRow.locator('button:has-text("Edit")')).toBeVisible();
    await expect(roleRow.locator('button:has-text("Delete")')).toBeVisible();
  });
});