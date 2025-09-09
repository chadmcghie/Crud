import { test, expect } from '../fixtures/authenticated-test';

/**
 * Tests for authenticated routes
 * Shows how to use auth helpers for protected endpoints
 */

test.describe('Authenticated Routes', () => {
  // This test uses the authenticatedPage fixture which auto-logs in
  test('can access people list when authenticated', async ({ page }) => {
    await page.goto('/people-list');
    
    // Should be able to see the people list (not redirected to login)
    await expect(page).toHaveURL(/.*people-list/);
    await expect(page.locator('app-people-list')).toBeVisible();
  });

  test('can access admin routes when authenticated as admin', async ({ page, authHelper }) => {
    // Login as admin
    await authHelper.loginAsAdmin();
    
    await page.goto('/roles');
    
    // Should be able to see the roles page (not redirected to unauthorized)
    await expect(page).toHaveURL(/.*roles/);
    await expect(page.locator('app-roles')).toBeVisible();
  });

  test('redirects to login when not authenticated', async ({ page, authHelper }) => {
    // Ensure we're logged out
    await authHelper.logout();
    
    // Try to access protected route
    await page.goto('/people-list');
    
    // Should be redirected to login
    await expect(page).toHaveURL(/.*login/);
  });

  test('shows unauthorized page for non-admin users', async ({ page, authHelper }) => {
    // Login as regular user
    await authHelper.login('user@example.com', 'User123!');
    
    // Try to access admin route
    await page.goto('/roles');
    
    // Should be redirected to unauthorized page
    await expect(page).toHaveURL(/.*unauthorized/);
  });
});