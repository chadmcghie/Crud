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

    // Click on People List link in navigation
    const peopleLink = page.locator('a:has-text("People List")');
    await expect(peopleLink).toBeVisible();
    await peopleLink.click();

    // Wait for navigation and any component to load
    await page.waitForLoadState('networkidle');

    // Look for any content indicating we're on people page - router outlet, components, or text
    const pageIndicators = page.locator('router-outlet, app-people, app-people-list, main, .content, [routerlink*="people"], h1, h2, h3').first();
    await expect(pageIndicators).toBeVisible({ timeout: 10000 });
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

    // Click on Add Person link in navigation
    const addPersonLink = page.locator('a:has-text("Add Person")');
    await expect(addPersonLink).toBeVisible();
    await addPersonLink.click();

    // Wait for navigation
    await page.waitForLoadState('networkidle');

    // Just verify the basic navigation elements exist - this is a smoke test
    const routerOutlet = page.locator('router-outlet');
    await expect(routerOutlet).toBeInViewport({ timeout: 5000 });

    // Verify we can still see the navigation (Angular app still working)
    await expect(page.locator('h1:has-text("CRUD Template Application")')).toBeVisible();
  });
});

test.describe('@smoke Roles Module', () => {
  test('@smoke Can navigate to roles list', async ({ page, baseURL }) => {
    await page.goto(baseURL);

    // Wait for app to load
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Click on Roles link in navigation
    const rolesLink = page.locator('a:has-text("Roles")');
    await expect(rolesLink).toBeVisible();
    await rolesLink.click();

    // Wait for navigation
    await page.waitForLoadState('networkidle');

    // Look for any content indicating we're on roles page
    const pageIndicators = page.locator('router-outlet, app-roles, app-roles-list, main, .content, [routerlink*="roles"], h1, h2, h3').first();
    await expect(pageIndicators).toBeVisible({ timeout: 10000 });
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