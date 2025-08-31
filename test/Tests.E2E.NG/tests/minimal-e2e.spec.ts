import { test, expect } from './setup/api-only-fixture';
import { generateTestPerson, generateTestRole } from './helpers/test-data';

test.describe('Minimal E2E Tests', () => {
  test('@smoke API server is running', async ({ apiContext }) => {
    const response = await apiContext.get('/health');
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    expect(data.status).toBe('Healthy');
  });

  test('@smoke GET /api/people/{id} - should get a person', async ({ apiHelpers }) => {
    // Create a person first
    const testPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    // Get the person
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    
    // Verify the person data matches
    expect(retrievedPerson).toMatchObject({
      id: createdPerson.id,
      fullName: createdPerson.fullName,
      phone: createdPerson.phone,
      roles: expect.any(Array)
    });
  });

  test('@smoke GET /api/roles/{id} - should get a role', async ({ apiHelpers }) => {
    // Create a role first
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    // Get the role
    const retrievedRole = await apiHelpers.getRole(createdRole.id);
    
    // Verify the role data matches
    expect(retrievedRole).toMatchObject({
      id: createdRole.id,
      name: createdRole.name,
      description: createdRole.description
    });
  });
});