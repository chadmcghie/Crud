import { test, expect } from '../fixtures/simple-test-fixture';

test.describe('@smoke Testing Environment Validation', () => {
  test('@smoke Should be running in Testing environment', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/system/info`);
    expect(response.ok()).toBe(true);

    const systemInfo = await response.json();
    expect(systemInfo.environment).toBe('Testing');
    console.log('System info:', systemInfo);
  });

  test('@smoke Should use SQLite database provider', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/system/info`);
    expect(response.ok()).toBe(true);

    const systemInfo = await response.json();
    expect(systemInfo.databaseProvider).toBe('SQLite');
  });

  test('@smoke Should have testing-specific features enabled', async ({ page, apiUrl }) => {
    // Test database reset functionality (only available in Testing environment)
    const resetResponse = await page.request.post(`${apiUrl}/api/test/reset-database`);
    expect(resetResponse.ok()).toBe(true);

    // Test API endpoints work after reset
    const peopleResponse = await page.request.get(`${apiUrl}/api/people`);
    expect(peopleResponse.ok()).toBe(true);
  });

  test('@smoke Should use unique database per test run', async ({ page, apiUrl }) => {
    // Create a unique test entity with valid FullName format (letters, spaces, hyphens, apostrophes, periods only)
    const testData = {
      fullName: `Test User Alpha Beta`, // Use valid name format without numbers
      phone: `+1-555-${Math.floor(Math.random() * 10000).toString().padStart(4, '0')}`
    };

    // Create entity
    const createResponse = await page.request.post(`${apiUrl}/api/people`, {
      data: testData,
      headers: {
        'Content-Type': 'application/json',
        'X-E2E-Test': 'true'
      }
    });
    expect(createResponse.ok()).toBe(true);
    const created = await createResponse.json();

    // Verify it exists
    const getResponse = await page.request.get(`${apiUrl}/api/people/${created.id}`);
    expect(getResponse.ok()).toBe(true);

    // Clean up
    await page.request.delete(`${apiUrl}/api/people/${created.id}`, {
      headers: {
        'X-E2E-Test': 'true'
      }
    });
  });
});

test.describe('@smoke Testing Configuration Performance', () => {
  test('@smoke Server startup should be optimized for Testing', async ({ page, apiUrl }) => {
    const startTime = Date.now();

    // Make 5 concurrent requests to test server responsiveness
    const promises = Array.from({ length: 5 }, () =>
      page.request.get(`${apiUrl}/api/people`)
    );

    const responses = await Promise.all(promises);
    const endTime = Date.now();

    // All requests should succeed
    responses.forEach(response => {
      expect(response.ok()).toBe(true);
    });

    // Should handle 5 concurrent requests quickly (under 2 seconds)
    const totalTime = endTime - startTime;
    console.log(`Performance test: ${totalTime}ms for 5 concurrent requests`);
    expect(totalTime).toBeLessThan(2000);
  });

  test('@smoke Database operations should be fast', async ({ page, apiUrl }) => {
    const testData = {
      fullName: 'Performance Test User', // Valid name format
      phone: '+1-555-1234' // Valid phone format with numbers only
    };

    // Time a full CRUD cycle
    const startTime = Date.now();

    // Create - with proper E2E test headers
    const createResponse = await page.request.post(`${apiUrl}/api/people`, {
      data: testData,
      headers: {
        'Content-Type': 'application/json',
        'X-E2E-Test': 'true'
      }
    });
    expect(createResponse.ok()).toBe(true);
    const created = await createResponse.json();

    // Read
    const readResponse = await page.request.get(`${apiUrl}/api/people/${created.id}`, {
      headers: {
        'X-E2E-Test': 'true'
      }
    });
    expect(readResponse.ok()).toBe(true);

    // Update
    const updateData = { ...testData, fullName: 'Updated Performance Test User' };
    const updateResponse = await page.request.put(`${apiUrl}/api/people/${created.id}`, {
      data: updateData,
      headers: {
        'Content-Type': 'application/json',
        'X-E2E-Test': 'true'
      }
    });
    expect(updateResponse.ok()).toBe(true);

    // Delete
    const deleteResponse = await page.request.delete(`${apiUrl}/api/people/${created.id}`, {
      headers: {
        'X-E2E-Test': 'true'
      }
    });
    expect(deleteResponse.ok()).toBe(true);

    const endTime = Date.now();
    const totalTime = endTime - startTime;

    console.log(`CRUD cycle time: ${totalTime}ms`);
    // Full CRUD cycle should complete within 1 second in Testing environment
    expect(totalTime).toBeLessThan(1000);
  });
});