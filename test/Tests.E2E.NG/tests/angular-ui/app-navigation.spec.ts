import { test, expect } from '@playwright/test';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';

test.describe('Application Navigation and Layout', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, request }) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(request);
    
    // Clean up any existing data
    if (apiHelpers) {
      try {
        await apiHelpers.cleanupAll();
      } catch (error) {
        console.warn('Failed to cleanup before test:', error);
      }
    }
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

  test('should load the application successfully', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Verify main title is displayed
    await pageHelpers.verifyPageTitle();
    
    // Verify subtitle is displayed
    await expect(page.locator('.subtitle')).toContainText('Comprehensive CRUD operations for managing people and their roles');
    
    // Verify tab buttons are present
    await expect(page.locator('button:has-text("👥 People Management")')).toBeVisible();
    await expect(page.locator('button:has-text("🎭 Roles Management")')).toBeVisible();
  });

  test('should have people tab active by default', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // People tab should be active
    await expect(page.locator('button:has-text("👥 People Management").active')).toBeVisible();
    
    // People content should be visible
    await expect(page.locator('app-people-list')).toBeVisible();
    
    // Roles content should not be visible
    await expect(page.locator('app-roles-list')).not.toBeVisible();
  });

  test('should switch between tabs correctly', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Start on people tab
    await expect(page.locator('app-people-list')).toBeVisible();
    await expect(page.locator('app-roles-list')).not.toBeVisible();
    
    // Switch to roles tab
    await pageHelpers.switchToRolesTab();
    await expect(page.locator('app-roles-list')).toBeVisible();
    await expect(page.locator('app-people-list')).not.toBeVisible();
    
    // Switch back to people tab
    await pageHelpers.switchToPeopleTab();
    await expect(page.locator('app-people-list')).toBeVisible();
    await expect(page.locator('app-roles-list')).not.toBeVisible();
  });

  test('should reset forms when switching tabs', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Open people form
    await pageHelpers.clickAddPerson();
    await expect(page.locator('app-people form')).toBeVisible();
    
    // Switch to roles tab
    await pageHelpers.switchToRolesTab();
    
    // Switch back to people tab
    await pageHelpers.switchToPeopleTab();
    
    // Form should be hidden
    await expect(page.locator('app-people form')).not.toBeVisible();
  });

  test('should maintain responsive design', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Test desktop view
    await page.setViewportSize({ width: 1400, height: 800 });
    await expect(page.locator('.management-layout')).toBeVisible();
    
    // Test tablet view
    await page.setViewportSize({ width: 768, height: 600 });
    await expect(page.locator('.management-layout')).toBeVisible();
    
    // Test mobile view
    await page.setViewportSize({ width: 400, height: 600 });
    await expect(page.locator('.management-layout')).toBeVisible();
  });

  test('should display correct tab indicators', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // People tab should be active initially
    const peopleTab = page.locator('button:has-text("👥 People Management")');
    const rolesTab = page.locator('button:has-text("🎭 Roles Management")');
    
    await expect(peopleTab).toHaveClass(/active/);
    await expect(rolesTab).not.toHaveClass(/active/);
    
    // Switch to roles tab
    await rolesTab.click();
    
    await expect(rolesTab).toHaveClass(/active/);
    await expect(peopleTab).not.toHaveClass(/active/);
  });

  test('should handle page refresh correctly', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Switch to roles tab
    await pageHelpers.switchToRolesTab();
    
    // Refresh the page
    await page.reload();
    await page.waitForLoadState('networkidle');
    
    // Should default back to people tab after refresh
    await expect(page.locator('app-people-list')).toBeVisible();
    await expect(page.locator('button:has-text("👥 People Management").active')).toBeVisible();
  });

  test('should display proper styling and layout', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Check main container styling
    const appContainer = page.locator('.app-container');
    await expect(appContainer).toBeVisible();
    
    // Check header styling
    const header = page.locator('.app-header');
    await expect(header).toBeVisible();
    await expect(header.locator('h1')).toHaveCSS('color', 'rgb(255, 255, 255)');
    
    // Check tab container styling
    const tabContainer = page.locator('.tab-container');
    await expect(tabContainer).toBeVisible();
    await expect(tabContainer).toHaveCSS('background-color', 'rgb(255, 255, 255)');
    
    // Check tab buttons styling
    const tabButtons = page.locator('.tab-buttons');
    await expect(tabButtons).toBeVisible();
  });

  test('should handle keyboard navigation', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Tab to the roles tab button
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    
    // Press Enter to activate roles tab
    await page.keyboard.press('Enter');
    
    // Should switch to roles tab
    await expect(page.locator('app-roles-list')).toBeVisible();
  });

  test('should display correct content sections', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Check people tab content
    await expect(page.locator('.list-section')).toBeVisible();
    await expect(page.locator('h3:has-text("People Directory")')).toBeVisible();
    
    // Switch to roles tab
    await pageHelpers.switchToRolesTab();
    
    // Check roles tab content
    await expect(page.locator('.list-section')).toBeVisible();
    await expect(page.locator('h3:has-text("Roles Management")')).toBeVisible();
  });

  test('should handle form section visibility', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Form section should not be visible initially
    await expect(page.locator('.form-section')).not.toBeVisible();
    
    // Click add person
    await pageHelpers.clickAddPerson();
    
    // Form section should now be visible
    await expect(page.locator('.form-section')).toBeVisible();
    
    // Cancel form
    await page.click('button:has-text("Cancel")');
    
    // Form section should be hidden again
    await expect(page.locator('.form-section')).not.toBeVisible();
  });
});