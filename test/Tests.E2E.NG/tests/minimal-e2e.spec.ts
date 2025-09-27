import { test, expect } from './setup/api-only-fixture';
import { generateTestPerson, generateTestRole } from './helpers/test-data';

test.describe('Minimal E2E Tests', () => {
  test('@smoke API server is running', async ({ apiContext, apiUrl }) => {
    console.log(`🔍 Testing API at: ${apiUrl}`);
    console.log(`🔍 Environment: CI=${process.env.CI}, API_URL=${process.env.API_URL}`);
    
    const response = await apiContext.get('/health');
    console.log(`📊 Health check response: ${response.status()}`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const json = await response.json();
    expect(json.status).toBe('Healthy');
  });

  test('@smoke GET /api/people/{id} - should get a person', async ({ apiHelpers, apiUrl }) => {
    console.log(`🔍 Person test - API URL: ${apiUrl}`);
    
    // Create a person first
    const testPerson = generateTestPerson();
    console.log(`📝 Creating person: ${JSON.stringify(testPerson)}`);
    
    let createdPerson;
    try {
      createdPerson = await apiHelpers.createPerson(testPerson);
      console.log(`✅ Person created with ID: ${createdPerson.id}`);
    } catch (error) {
      console.error(`❌ Failed to create person: ${error}`);
      throw error;
    }
    
    // Get the person
    console.log(`📖 Getting person with ID: ${createdPerson.id}`);
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    console.log(`✅ Retrieved person: ${JSON.stringify(retrievedPerson)}`);
    
    // Verify the person data matches
    expect(retrievedPerson).toMatchObject({
      id: createdPerson.id,
      fullName: createdPerson.fullName,
      phone: createdPerson.phone,
      roles: expect.any(Array)
    });
  });

  test('@smoke GET /api/roles/{id} - should get a role', async ({ apiHelpers, apiUrl }) => {
    console.log(`🔍 Role test - API URL: ${apiUrl}`);
    
    // Create a role first
    const testRole = generateTestRole();
    console.log(`📝 Creating role: ${JSON.stringify(testRole)}`);
    
    let createdRole;
    try {
      createdRole = await apiHelpers.createRole(testRole);
      console.log(`✅ Role created with ID: ${createdRole.id}`);
    } catch (error) {
      console.error(`❌ Failed to create role: ${error}`);
      throw error;
    }
    
    // Get the role
    console.log(`📖 Getting role with ID: ${createdRole.id}`);
    const retrievedRole = await apiHelpers.getRole(createdRole.id);
    console.log(`✅ Retrieved role: ${JSON.stringify(retrievedRole)}`);
    
    // Verify the role data matches
    expect(retrievedRole).toMatchObject({
      id: createdRole.id,
      name: createdRole.name,
      description: createdRole.description
    });
  });
});
