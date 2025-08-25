import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, testRoles } from '../helpers/test-data';

test.describe('Roles API', () => {
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ request }) => {
    apiHelpers = new ApiHelpers(request);
    // Clean up any existing data
    await apiHelpers.cleanupAll();
  });

  test.afterEach(async () => {
    // Clean up after each test
    await apiHelpers.cleanupAll();
  });

  test('GET /api/roles - should return empty array when no roles exist', async () => {
    const roles = await apiHelpers.getRoles();
    expect(roles).toEqual([]);
  });

  test('POST /api/roles - should create a new role successfully', async () => {
    const testRole = generateTestRole();
    
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole).toMatchObject({
      id: expect.any(String),
      name: testRole.name,
      description: testRole.description
    });
    
    // Verify the role exists in the list
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(1);
    expect(roles[0]).toMatchObject(createdRole);
  });

  test('POST /api/roles - should create role with only required fields', async () => {
    const testRole = { name: 'Test Role Required Only' };
    
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole).toMatchObject({
      id: expect.any(String),
      name: testRole.name,
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
    
    expect(response.status()).toBe(404);
  });

  test('DELETE /api/roles/{id} - should delete existing role', async () => {
    const testRole = generateTestRole();
    const createdRole = await apiHelpers.createRole(testRole);
    
    // Verify role exists
    let roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(1);
    
    // Delete the role
    await apiHelpers.deleteRole(createdRole.id);
    
    // Verify role no longer exists
    roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(0);
  });

  test('DELETE /api/roles/{id} - should return 404 for non-existent role', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.delete(`/api/roles/${nonExistentId}`);
    expect(response.status()).toBe(404);
  });

  test('should handle multiple roles correctly', async () => {
    const createdRoles = [];
    
    // Create multiple roles
    for (const roleData of testRoles) {
      const createdRole = await apiHelpers.createRole(roleData);
      createdRoles.push(createdRole);
    }
    
    // Verify all roles exist
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(testRoles.length);
    
    // Verify each role
    for (const createdRole of createdRoles) {
      const foundRole = roles.find(r => r.id === createdRole.id);
      expect(foundRole).toMatchObject(createdRole);
    }
  });

  test('should maintain data integrity during concurrent operations', async () => {
    const testRole1 = generateTestRole();
    const testRole2 = generateTestRole();
    
    // Create roles concurrently
    const [createdRole1, createdRole2] = await Promise.all([
      apiHelpers.createRole(testRole1),
      apiHelpers.createRole(testRole2)
    ]);
    
    // Verify both roles exist
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(2);
    
    const foundRole1 = roles.find(r => r.id === createdRole1.id);
    const foundRole2 = roles.find(r => r.id === createdRole2.id);
    
    expect(foundRole1).toMatchObject(createdRole1);
    expect(foundRole2).toMatchObject(createdRole2);
  });

  test('should handle role name uniqueness', async () => {
    const roleName = 'Unique Role Name';
    const testRole1 = generateTestRole({ name: roleName });
    const testRole2 = generateTestRole({ name: roleName });
    
    // Create first role
    await apiHelpers.createRole(testRole1);
    
    // Try to create second role with same name
    // Note: This test assumes the API should handle duplicate names gracefully
    // If the API enforces uniqueness, this should fail with appropriate error
    try {
      await apiHelpers.createRole(testRole2);
      
      // If creation succeeds, verify both roles exist
      const roles = await apiHelpers.getRoles();
      expect(roles.filter(r => r.name === roleName)).toHaveLength(2);
    } catch (error: any) {
      // If creation fails due to uniqueness constraint, that's also valid
      expect(error.message).toContain('Failed to create role');
    }
  });

  test('should handle special characters in role data', async () => {
    const testRole = generateTestRole({
      name: 'Role with Special Characters: !@#$%^&*()',
      description: 'Description with unicode: 你好 🌟 émojis and symbols'
    });
    
    const createdRole = await apiHelpers.createRole(testRole);
    
    expect(createdRole.name).toBe(testRole.name);
    expect(createdRole.description).toBe(testRole.description);
    
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
    expect(createdRole.description).toBe(largeDescription);
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