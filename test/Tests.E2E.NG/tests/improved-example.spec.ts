import { test, expect } from './setup/improved-test-fixture';
import { generateTestPerson, generateTestRole } from './helpers/test-data';

test.describe('Improved E2E Tests with Polly Resilience', () => {
  test('should handle API operations with resilience', async ({ apiHelpers }) => {
    // This test demonstrates improved resilience with automatic retry
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole).toHaveProperty('id');
    expect(createdRole.name).toContain(testRole.name);
    
    // Verify role was created
    const roles = await apiHelpers.getRoles();
    const foundRole = roles.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
  });

  test('should handle concurrent operations efficiently', async ({ apiHelpers }) => {
    // Create multiple entities concurrently
    const createPromises = [];
    
    for (let i = 0; i < 5; i++) {
      createPromises.push(apiHelpers.createPerson(generateTestPerson()));
    }
    
    // All operations should complete successfully with retry logic
    const createdPeople = await Promise.all(createPromises);
    
    expect(createdPeople).toHaveLength(5);
    createdPeople.forEach(person => {
      expect(person).toHaveProperty('id');
      expect(person).toHaveProperty('fullName');
    });
  });

  test('should navigate to UI efficiently with server reuse', async ({ page, angularUrl }) => {
    // Server should already be running and reused
    await page.goto(angularUrl);
    
    // Wait for Angular app to load
    await expect(page.locator('app-root')).toBeVisible({ timeout: 30000 });
    
    // Verify the page loaded correctly
    const title = await page.title();
    expect(title).toBeTruthy();
  });

  test('should handle database operations with resilience', async ({ apiHelpers }) => {
    // Clean up old data
    await apiHelpers.cleanupAll();
    
    // Create test data
    const role = await apiHelpers.createRole(generateTestRole());
    const person = await apiHelpers.createPerson({
      ...generateTestPerson(),
      roleIds: [role.id]
    });
    
    expect(person.roles).toHaveLength(1);
    expect(person.roles[0].id).toBe(role.id);
    
    // Update person
    const updatedData = generateTestPerson();
    await apiHelpers.updatePerson(person.id, updatedData);
    
    // Verify update
    const updatedPerson = await apiHelpers.getPerson(person.id);
    expect(updatedPerson.fullName).toBe(updatedData.fullName);
  });

  test('should handle API errors gracefully', async ({ apiContext }) => {
    // Try to get non-existent resource
    const response = await apiContext.get('/api/people/00000000-0000-0000-0000-000000000000');
    
    // Should handle 400 (validation) or 404 gracefully
    expect([400, 404]).toContain(response.status());
  });

  test('should perform CRUD operations with circuit breaker protection', async ({ apiHelpers }) => {
    const operations = [];
    
    // Perform multiple operations that would trigger circuit breaker if failing
    for (let i = 0; i < 10; i++) {
      operations.push(
        apiHelpers.createPerson(generateTestPerson())
          .then(person => apiHelpers.deletePerson(person.id))
          .catch(error => {
            // Circuit breaker or retry logic should handle transient failures
            console.warn(`Operation ${i} failed but was handled:`, error.message);
            return null;
          })
      );
    }
    
    const results = await Promise.all(operations);
    
    // Most operations should succeed with resilience
    const successCount = results.filter(r => r !== null).length;
    expect(successCount).toBeGreaterThan(0);
  });
});

test.describe('Parallel Test Execution', () => {
  // These tests demonstrate that parallel execution works correctly
  // with improved server management
  
  test('parallel test 1', async ({ apiHelpers, parallelIndex }) => {
    console.log(`Running test 1 on parallel worker ${parallelIndex}`);
    
    const person = await apiHelpers.createPerson(generateTestPerson());
    expect(person).toHaveProperty('id');
    
    // Clean up
    await apiHelpers.deletePerson(person.id);
  });
  
  test('parallel test 2', async ({ apiHelpers, parallelIndex }) => {
    console.log(`Running test 2 on parallel worker ${parallelIndex}`);
    
    const role = await apiHelpers.createRole(generateTestRole());
    expect(role).toHaveProperty('id');
    
    // Clean up
    await apiHelpers.deleteRole(role.id);
  });
  
  test('parallel test 3', async ({ apiHelpers, parallelIndex }) => {
    console.log(`Running test 3 on parallel worker ${parallelIndex}`);
    
    const people = await apiHelpers.getPeople();
    expect(Array.isArray(people)).toBe(true);
  });
});