import { test, expect } from '../setup/test-fixture';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';

test.describe('Application Navigation and Layout', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, apiContext, cleanDatabase }) => {
    // cleanDatabase fixture handles database cleanup automatically
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(apiContext, 0); // Serial execution - single worker
    
    console.log(`🧪 Starting test - database automatically cleaned`);
  });

  test('should load the application successfully', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Verify main title is displayed
    await pageHelpers.verifyPageTitle();
    
    // Verify navigation links are present
    await expect(page.locator('a[routerLink="/people-list"]')).toBeVisible();
    await expect(page.locator('a[routerLink="/roles-list"]')).toBeVisible();
  });

  test('should navigate to people page', async ({ page }) => {
    await pageHelpers.navigateToApp();
    await pageHelpers.switchToPeopleTab();
    
    // People content should be visible
    await expect(page.locator('app-people-list')).toBeVisible();
  });

  test('should switch between pages correctly', async ({ page }) => {
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

  test('should reset forms when switching pages', async ({ page }) => {
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

  test('should display navigation links', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Navigation links should be visible
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    const rolesLink = page.locator('a[routerLink="/roles-list"]');
    
    await expect(peopleLink).toBeVisible();
    await expect(rolesLink).toBeVisible();
  });

  test('should handle page refresh correctly', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Switch to roles tab
    await pageHelpers.switchToRolesTab();
    
    // Refresh the page
    await page.reload();
    // Wait for specific content instead of networkidle
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 30000 });
    await page.waitForSelector('a[routerLink="/people-list"]', { timeout: 15000 });
    
    // Navigate to people page after refresh
    await pageHelpers.switchToPeopleTab();
    await expect(page.locator('app-people-list')).toBeVisible();
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
    
    // Check main content styling
    const mainContent = page.locator('.main-content');
    await expect(mainContent).toBeVisible();
  });

  test('should handle keyboard navigation', async ({ page }) => {
    await pageHelpers.navigateToApp();
    
    // Focus on the roles tab button specifically
    await page.locator('button:has-text("🎭 Roles Management")').focus();
    
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
