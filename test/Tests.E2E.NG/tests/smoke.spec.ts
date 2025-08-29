import { test, expect } from './fixtures/serial-test-fixture';

/**
 * Smoke Test Suite
 * Quick validation that the application is running and core features work
 * Target: < 2 minutes total runtime
 * 
 * These tests should:
 * - Run fast
 * - Cover critical paths
 * - Fail fast if something is fundamentally broken
 * - Not require complex setup
 */

test.describe('@smoke Application Health Checks', () => {
  test('@smoke API server is running', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/health`);
    expect(response.ok()).toBe(true);
  });
  
  test('@smoke Angular application loads', async ({ page, baseURL }) => {
    const response = await page.goto(baseURL);
    expect(response?.ok()).toBe(true);
    
    // Check that Angular bootstrapped
    await page.waitForFunction(
      () => typeof (window as any).ng !== 'undefined',
      { timeout: 10000 }
    );
  });
  
  test('@smoke Navigation menu is visible', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    
    // Check for navigation elements
    const nav = page.locator('nav, [role="navigation"]').first();
    await expect(nav).toBeVisible();
    
    // Check for main navigation links
    const peopleLink = page.locator('a[href*="people"], a:has-text("People")').first();
    const rolesLink = page.locator('a[href*="roles"], a:has-text("Roles")').first();
    
    await expect(peopleLink).toBeVisible();
    await expect(rolesLink).toBeVisible();
  });
});

test.describe('@smoke People Module', () => {
  test('@smoke Can navigate to people list', async ({ page, baseURL }) => {
    await page.goto(`${baseURL}/people`);
    
    // Page should load without errors
    await expect(page).toHaveURL(/people/);
    
    // Check for people list container
    const listContainer = page.locator('table, .list, [role="grid"]').first();
    await expect(listContainer).toBeVisible({ timeout: 5000 });
  });
  
  test('@smoke People API endpoint responds', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/people`);
    expect(response.ok()).toBe(true);
    
    const data = await response.json();
    expect(Array.isArray(data)).toBe(true);
  });
  
  test('@smoke Can open add person form', async ({ page, baseURL }) => {
    await page.goto(`${baseURL}/people`);
    
    // Click add button
    const addButton = page.locator('button:has-text("Add"), a:has-text("Add"), button:has-text("New")').first();
    await expect(addButton).toBeVisible();
    await addButton.click();
    
    // Form should be visible
    const form = page.locator('form').first();
    await expect(form).toBeVisible({ timeout: 5000 });
    
    // Check for essential form fields
    const nameField = page.locator('input[name="fullName"], input[placeholder*="name" i]').first();
    await expect(nameField).toBeVisible();
  });
});

test.describe('@smoke Roles Module', () => {
  test('@smoke Can navigate to roles list', async ({ page, baseURL }) => {
    await page.goto(`${baseURL}/roles`);
    
    // Page should load without errors
    await expect(page).toHaveURL(/roles/);
    
    // Check for roles list container
    const listContainer = page.locator('table, .list, [role="grid"]').first();
    await expect(listContainer).toBeVisible({ timeout: 5000 });
  });
  
  test('@smoke Roles API endpoint responds', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/roles`);
    expect(response.ok()).toBe(true);
    
    const data = await response.json();
    expect(Array.isArray(data)).toBe(true);
  });
});

test.describe('@smoke Database Operations', () => {
  test('@smoke Can perform CRUD cycle', async ({ page, apiUrl }) => {
    const timestamp = Date.now();
    const testData = {
      fullName: `Smoke Test ${timestamp}`,
      phone: '555-SMOKE'
    };
    
    // Create
    const createResponse = await page.request.post(`${apiUrl}/api/people`, {
      data: testData
    });
    expect(createResponse.ok()).toBe(true);
    const created = await createResponse.json();
    expect(created.id).toBeTruthy();
    
    // Read
    const readResponse = await page.request.get(`${apiUrl}/api/people/${created.id}`);
    expect(readResponse.ok()).toBe(true);
    const read = await readResponse.json();
    expect(read.fullName).toBe(testData.fullName);
    
    // Update
    const updateData = { ...created, fullName: `${testData.fullName} Updated` };
    const updateResponse = await page.request.put(`${apiUrl}/api/people/${created.id}`, {
      data: updateData
    });
    expect(updateResponse.ok()).toBe(true);
    
    // Delete
    const deleteResponse = await page.request.delete(`${apiUrl}/api/people/${created.id}`);
    expect(deleteResponse.ok()).toBe(true);
    
    // Verify deletion
    const verifyResponse = await page.request.get(`${apiUrl}/api/people/${created.id}`);
    expect(verifyResponse.status()).toBe(404);
  });
});

test.describe('@smoke Error Handling', () => {
  test('@smoke API returns 404 for non-existent resource', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/people/non-existent-id-12345`);
    expect(response.status()).toBe(404);
  });
  
  test('@smoke API validates required fields', async ({ page, apiUrl }) => {
    const response = await page.request.post(`${apiUrl}/api/people`, {
      data: { /* empty object - missing required fields */ }
    });
    
    // Should return 400 Bad Request or 422 Unprocessable Entity
    expect([400, 422]).toContain(response.status());
  });
});