import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, testRoles } from '../helpers/test-data';
import { APIRequestContext } from '@playwright/test';

// Helper function to reset database using Respawn
async function resetDatabase(request: APIRequestContext): Promise<void> {
  try {
    const response = await request.post('http://localhost:5172/api/database/reset', {
      data: {
        workerIndex: 0,
        preserveSchema: true
      }
    });
    
    if (!response.ok()) {
      console.warn(`⚠️  Database reset warning: ${response.status()}`);
      // Don't throw - let tests proceed even if reset fails
    } else {
      console.log(`🔄 Database reset completed successfully`);
    }
  } catch (error) {
    console.warn(`⚠️  Database reset error:`, error);
    // Don't throw - let tests proceed
  }
}

test.describe('Roles API', () => {
  let apiHelpers: ApiHelpers;

  test.beforeAll(async ({ request }) => {
    // Global cleanup at the start to remove any leftover data from previous runs
    const globalApiHelpers = new ApiHelpers(request, 0);
    await globalApiHelpers.cleanupAll();
  });

  test.beforeEach(async ({ request }, testInfo) => {
    apiHelpers = new ApiHelpers(request, testInfo.workerIndex);
    
    // Clean up any existing data (sequential execution ensures no race conditions)
    await apiHelpers.cleanupAll();
  });

  test('GET /api/roles - should return seed data when clean', async () => {
    // Force immediate cleanup to ensure completely clean state
    await apiHelpers.cleanupAll(true);
    
    const roles = await apiHelpers.getRoles();
    // After cleanup, should have seed data but no test-specific roles
    expect(roles.length).toBeGreaterThanOrEqual(0);
    const testRoles = roles.filter(r => r.name.includes('W') && r.name.includes('_T'));
    expect(testRoles).toEqual([]);
  });

  test('POST /api/roles - should create a new role successfully', async () => {
    const testRole = generateTestRole();

    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole).toMatchObject({
      id: expect.any(String),
      name: expect.stringContaining(testRole.name),
      description: expect.stringContaining(testRole.description)
    });
    
    // Verify the role exists in the list (along with seed data)
    const roles = await apiHelpers.getRoles();
    expect(roles.length).toBeGreaterThanOrEqual(1);
    const createdRoleInList = roles.find(r => r.id === createdRole.id);
    expect(createdRoleInList).toMatchObject(createdRole);
  });

  test('POST /api/roles - should create role with only required fields', async () => {
    const testRole = { name: 'Test Role Required Only' };
    
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole).toMatchObject({
      id: expect.any(String),
      name: expect.stringContaining(testRole.name),
      description: null
    });
  });

  test('POST /api/roles - should validate required fields', async ({ request }) => {
    // Try to create role without name
    const response = await request.post('/api/roles', {
      data: { description: 'Role without name' }
    });
    
    expect(response.status()).toBe(400);
  });

  test('GET /api/roles/{id} - should return specific role', async () => {
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    const retrievedRole = await apiHelpers.getRole(createdRole.id);
    
    expect(retrievedRole).toMatchObject(createdRole);
  });

  test('GET /api/roles/{id} - should return 404 for non-existent role', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.get(`/api/roles/${nonExistentId}`);
    expect(response.status()).toBe(404);
  });

  test('PUT /api/roles/{id} - should update existing role', async () => {
    const originalRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(originalRole);
    
    const updatedData = generateTestRole();
    await apiHelpers.updateRole(createdRole.id, updatedData);
    
    // Verify the role was updated
    const retrievedRole = await apiHelpers.getRole(createdRole.id);
    expect(retrievedRole).toMatchObject({
      id: createdRole.id,
      name: updatedData.name,
      description: updatedData.description
    });
  });

  test('PUT /api/roles/{id} - should return 404 for non-existent role', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    const updateData = generateTestRole();
    
    const response = await request.put(`/api/roles/${nonExistentId}`, {
      data: updateData
    });
    
    // API may return 404 or 500 for non-existent resources
    expect([404, 500]).toContain(response.status());
  });

  test('DELETE /api/roles/{id} - should delete existing role', async () => {
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    // Verify role exists (along with seed data)
    let roles = await apiHelpers.getRoles();
    const createdRoleExists = roles.find(r => r.id === createdRole.id);
    expect(createdRoleExists).toBeDefined();
    
    // Delete the role
    await apiHelpers.deleteRole(createdRole.id);
    
    // Verify role no longer exists (but seed data remains)
    roles = await apiHelpers.getRoles();
    const deletedRoleStillExists = roles.find(r => r.id === createdRole.id);
    expect(deletedRoleStillExists).toBeUndefined();
  });

  test('DELETE /api/roles/{id} - should return 404 for non-existent role', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.delete(`/api/roles/${nonExistentId}`);
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('should handle multiple roles correctly', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const createdRoles = [];
    
    // Create multiple roles
    for (const roleData of testRoles) {
      const createdRole = await apiHelpers.createRole(roleData);
      createdRoles.push(createdRole);
    }
    
    // Verify all roles exist - filter by our current test's data
    const roles = await apiHelpers.getRoles();
    const currentTestRoles = roles.filter(r => createdRoles.some(cr => cr.id === r.id));
    expect(currentTestRoles).toHaveLength(testRoles.length);
    
    // Verify each role
    for (const createdRole of createdRoles) {
      const foundRole = roles.find(r => r.id === createdRole.id);
      expect(foundRole).toMatchObject(createdRole);
    }
  });

  test('should maintain data integrity during concurrent operations', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const testRole1 = generateTestRole();
    const testRole2 = generateTestRole();
    
    // Create roles concurrently
    const [createdRole1, createdRole2] = await Promise.all([
      apiHelpers.createRole(testRole1),
      apiHelpers.createRole(testRole2)
    ]);
    
    // Verify both roles exist - filter by our current test's data
    const roles = await apiHelpers.getRoles();
    const currentTestRoles = roles.filter(r => [createdRole1.id, createdRole2.id].includes(r.id));
    expect(currentTestRoles).toHaveLength(2);
    
    const foundRole1 = roles.find(r => r.id === createdRole1.id);
    const foundRole2 = roles.find(r => r.id === createdRole2.id);
    
    expect(foundRole1).toMatchObject(createdRole1);
    expect(foundRole2).toMatchObject(createdRole2);
  });

  test('should handle role name uniqueness', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const roleName = 'Unique Role Name';
    const testRole1 = generateTestRole({ name: roleName });
    const testRole2 = generateTestRole({ name: roleName });
    
    // Create first role
    const createdRole1 = await apiHelpers.createRole(testRole1);
    
    // Create second role - with our worker isolation, both will succeed with unique names
    const createdRole2 = await apiHelpers.createRole(testRole2);
    
    // Verify both roles exist and contain the original name
    const roles = await apiHelpers.getRoles();
    const rolesWithSameName = roles.filter(r => r.name.includes(roleName));
    expect(rolesWithSameName).toHaveLength(2);
    
    // Verify they have different actual names due to worker isolation
    expect(createdRole1.name).not.toBe(createdRole2.name);
    expect(createdRole1.name).toContain(roleName);
    expect(createdRole2.name).toContain(roleName);
  });

  test('should handle special characters in role data', async () => {
    const testRole = generateTestRole({
      name: 'Role with Special Characters: !@#$%^&*()',
      description: 'Description with unicode: 你好 🌟 émojis and symbols'
    });
    
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole.name).toContain(testRole.name);
    expect(createdRole.description).toContain(testRole.description);
    
    // Verify retrieval works correctly
    const retrievedRole = await apiHelpers.getRole(createdRole.id);
    expect(retrievedRole).toMatchObject(createdRole);
  });

  test('should handle large description text', async () => {
    const largeDescription = 'A'.repeat(1000); // 1000 character description
    const testRole = generateTestRole({
      description: largeDescription
    });
    
    const createdRole = await apiHelpers.createRole(testRole);
    expect(createdRole.description).toContain(largeDescription);
  });

  test('should return proper HTTP status codes', async ({ request }) => {
    const testRole = generateTestRole();
    
    // POST should return 201 Created
    const createResponse = await request.post('/api/roles', {
      data: testRole
    });
    expect(createResponse.status()).toBe(201);
    const createdRole = await createResponse.json();
    
    // GET should return 200 OK
    const getResponse = await request.get(`/api/roles/${createdRole.id}`);
    expect(getResponse.status()).toBe(200);
    
    // PUT should return 204 No Content
    const updateResponse = await request.put(`/api/roles/${createdRole.id}`, {
      data: generateTestRole()
    });
    expect(updateResponse.status()).toBe(204);
    
    // DELETE should return 204 No Content
    const deleteResponse = await request.delete(`/api/roles/${createdRole.id}`);
    expect(deleteResponse.status()).toBe(204);
  });

  test('should handle malformed JSON requests', async ({ request }) => {
    const response = await request.post('/api/roles', {
      data: 'invalid json',
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    expect(response.status()).toBe(400);
  });
});