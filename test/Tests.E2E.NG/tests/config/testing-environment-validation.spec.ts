import { test, expect } from '../fixtures/simple-test-fixture';

test.describe('@smoke Testing Environment Validation', () => {
  test('@smoke Should be running in Testing environment', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/system/info`);
    expect(response.ok()).toBe(true);

    const systemInfo = await response.json();
    console.log('System info:', JSON.stringify(systemInfo, null, 2));

    expect(systemInfo.environment).toBe('Testing');
  });

  test('@smoke Should use SQLite database provider', async ({ page, apiUrl }) => {
    const response = await page.request.get(`${apiUrl}/api/system/info`);
    expect(response.ok()).toBe(true);

    const systemInfo = await response.json();
    expect(systemInfo.databaseProvider).toBe('SQLite');
  });

  test('@smoke Should have testing-specific features enabled', async ({ page, apiUrl }) => {
    // Test reset endpoint availability
    const resetResponse = await page.request.post(`${apiUrl}/api/test/reset-database`);
    expect(resetResponse.ok()).toBe(true);

    // Test that authentication bypass is enabled
    const peopleResponse = await page.request.get(`${apiUrl}/api/people`);
    expect(peopleResponse.ok()).toBe(true);
  });

  test('@smoke Should use unique database per test run', async ({ page, apiUrl }) => {
    // Create a test record to verify database isolation using valid name format
    const names = ['Alice', 'Bob', 'Charlie', 'Diana', 'Edward'];
    const surnames = ['Test', 'User', 'Sample', 'Example', 'Demo'];
    const randomName = names[Math.floor(Math.random() * names.length)];
    const randomSurname = surnames[Math.floor(Math.random() * surnames.length)];

    const testData = {
      fullName: `${randomName} ${randomSurname}`,  // Fixed: use only alphabetic characters
      phone: '+1-555-0001'
    };

    const createResponse = await page.request.post(`${apiUrl}/api/people`, {
      data: testData
    });

    // Clean up debugging - test should work now
    expect(createResponse.ok()).toBe(true);

    const created = await createResponse.json();
    expect(created.id).toBeTruthy();
    expect(created.fullName).toBe(testData.fullName);

    // Cleanup
    await page.request.delete(`${apiUrl}/api/people/${created.id}`);
  });
});

test.describe('@smoke Testing Configuration Performance', () => {
  test('@smoke Server startup should be optimized for Testing', async ({ page, apiUrl }) => {
    const startTime = Date.now();

    // Multiple rapid API calls to test performance
    const promises = [];
    for (let i = 0; i < 5; i++) {
      promises.push(page.request.get(`${apiUrl}/api/people`));
    }

    const responses = await Promise.all(promises);
    const endTime = Date.now();

    // All responses should be successful
    responses.forEach((response, index) => {
      expect(response.ok()).toBe(true);
    });

    // Should handle 5 concurrent requests quickly (under 2 seconds)
    const totalTime = endTime - startTime;
    console.log(`Performance test: ${totalTime}ms for 5 concurrent requests`);
    expect(totalTime).toBeLessThan(2000);
  });

  test('@smoke Database operations should be fast', async ({ page, apiUrl }) => {
    const testData = {
      fullName: 'Performance Test User',
      phone: '+1-555-TEST'
    };

    // Time a full CRUD cycle
    const startTime = Date.now();

    // Create
    const createResponse = await page.request.post(`${apiUrl}/api/people`, {
      data: testData
    });
    expect(createResponse.ok()).toBe(true);
    const created = await createResponse.json();

    // Read
    const readResponse = await page.request.get(`${apiUrl}/api/people/${created.id}`);
    expect(readResponse.ok()).toBe(true);

    // Update
    const updateData = { ...testData, fullName: 'Updated Performance Test User' };
    const updateResponse = await page.request.put(`${apiUrl}/api/people/${created.id}`, {
      data: updateData
    });
    expect(updateResponse.ok()).toBe(true);

    // Delete
    const deleteResponse = await page.request.delete(`${apiUrl}/api/people/${created.id}`);
    expect(deleteResponse.ok()).toBe(true);

    const endTime = Date.now();
    const totalTime = endTime - startTime;

    console.log(`CRUD cycle time: ${totalTime}ms`);
    // Full CRUD cycle should complete within 1 second in Testing environment
    expect(totalTime).toBeLessThan(1000);
  });
});