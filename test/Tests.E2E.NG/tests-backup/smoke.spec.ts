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
    
    // Wait for the app to load
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });
    
    // Check for navigation links
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    const rolesLink = page.locator('a[routerLink="/roles-list"]');
    
    await expect(peopleLink).toBeVisible();
    await expect(rolesLink).toBeVisible();
  });
});

test.describe('@smoke People Module', () => {
  test('@smoke Can navigate to people list', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    
    // Wait for app to load
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });
    
    // Click on People link
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    await peopleLink.click();
    
    // Check for people list container
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    const listContainer = page.locator('.people-table, app-people-list').first();
    await expect(listContainer).toBeVisible();
  });
  
  test('@smoke People API endpoint responds', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/people`);
    expect(response.ok()).toBe(true);
    
    const data = await response.json();
    expect(Array.isArray(data)).toBe(true);
  });
  
  test('@smoke Can open add person form', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    
    // Wait for app to load
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });
    
    // Click on People link
    const peopleLink = page.locator('a[routerLink="/people-list"]');
    await peopleLink.click();
    await page.locator('app-people-list').waitFor({ state: 'visible', timeout: 5000 });
    
    // Click add button
    const addButton = page.locator('button:has-text("Add New Person")');
    await expect(addButton).toBeVisible();
    await addButton.click();
    
    // Form should be visible
    await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });
    const form = page.locator('app-people form').first();
    await expect(form).toBeVisible();
    
    // Check for essential form fields
    const nameField = page.locator('input#fullName');
    await expect(nameField).toBeVisible();
  });
});

test.describe('@smoke Roles Module', () => {
  test('@smoke Can navigate to roles list', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    
    // Wait for app to load
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });
    
    // Click on Roles link
    const rolesLink = page.locator('a[routerLink="/roles-list"]');
    await rolesLink.click();
    
    // Wait for roles component to load
    await page.locator('app-roles-list').waitFor({ state: 'visible', timeout: 5000 });
    
    // Check for roles list container
    const listContainer = page.locator('.roles-table, app-roles-list').first();
    await expect(listContainer).toBeVisible();
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
      fullName: `John Smoke Smith`,  // Use a valid name format without timestamp
      phone: '+1-555-9999'  // Use valid phone format
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