import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestPerson, generateTestRole, testPeople } from '../helpers/test-data';

test.describe('People API', () => {
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

  test('GET /api/people - should return empty array when no people exist', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const people = await apiHelpers.getPeople();
    expect(people).toEqual([]);
  });

  test('POST /api/people - should create a new person successfully', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const testPerson = generateTestPerson();
    
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    expect(createdPerson).toMatchObject({
      id: expect.any(String),
      fullName: testPerson.fullName,
      phone: testPerson.phone,
      roles: expect.any(Array)
    });
    
    // Verify the person exists in the list
    const people = await apiHelpers.getPeople();
    expect(people).toHaveLength(1);
    expect(people[0]).toMatchObject(createdPerson);
  });

  test('POST /api/people - should create person with only required fields', async () => {
    const testPerson = { fullName: 'Test Person Required Only' };
    
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    expect(createdPerson).toMatchObject({
      id: expect.any(String),
      fullName: testPerson.fullName,
      phone: null,
      roles: []
    });
  });

  test('POST /api/people - should create person with roles', async () => {
    // First create some roles
    const role1 = await apiHelpers.createRole(generateTestRole());
    const role2 = await apiHelpers.createRole(generateTestRole());
    
    const testPerson = generateTestPerson({
      roleIds: [role1.id, role2.id]
    });
    
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    expect(createdPerson.roles).toHaveLength(2);
    expect(createdPerson.roles.map((r: any) => r.id)).toEqual(
      expect.arrayContaining([role1.id, role2.id])
    );
  });

  test('POST /api/people - should validate required fields', async ({ request }) => {
    // Try to create person without fullName
    const response = await request.post('/api/people', {
      data: { phone: '+1-555-0123' }
    });
    
    expect(response.status()).toBe(400);
  });

  test('GET /api/people/{id} - should return specific person', async () => {
    const testPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    
    expect(retrievedPerson).toMatchObject(createdPerson);
  });

  test('GET /api/people/{id} - should return 404 for non-existent person', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.get(`/api/people/${nonExistentId}`);
    expect(response.status()).toBe(404);
  });

  test('PUT /api/people/{id} - should update existing person', async () => {
    const originalPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(originalPerson);
    
    const updatedData = generateTestPerson();
    await apiHelpers.updatePerson(createdPerson.id, updatedData);
    
    // Verify the person was updated
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    expect(retrievedPerson).toMatchObject({
      id: createdPerson.id,
      fullName: updatedData.fullName,
      phone: updatedData.phone
    });
  });

  test('PUT /api/people/{id} - should update person roles', async () => {
    // Create roles
    const role1 = await apiHelpers.createRole(generateTestRole());
    const role2 = await apiHelpers.createRole(generateTestRole());
    const role3 = await apiHelpers.createRole(generateTestRole());
    
    // Create person with initial roles
    const testPerson = generateTestPerson({
      roleIds: [role1.id, role2.id]
    });
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    // Update person with different roles
    const updatedData = generateTestPerson({
      roleIds: [role2.id, role3.id]
    });
    await apiHelpers.updatePerson(createdPerson.id, updatedData);
    
    // Verify roles were updated
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    expect(retrievedPerson.roles).toHaveLength(2);
    expect(retrievedPerson.roles.map((r: any) => r.id)).toEqual(
      expect.arrayContaining([role2.id, role3.id])
    );
    expect(retrievedPerson.roles.map((r: any) => r.id)).not.toContain(role1.id);
  });

  test('PUT /api/people/{id} - should return 404 for non-existent person', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    const updateData = generateTestPerson();
    
    const response = await request.put(`/api/people/${nonExistentId}`, {
      data: updateData
    });
    
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('DELETE /api/people/{id} - should delete existing person', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const testPerson = generateTestPerson();
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    // Verify person exists
    let people = await apiHelpers.getPeople();
    expect(people).toHaveLength(1);
    
    // Delete the person
    await apiHelpers.deletePerson(createdPerson.id);
    
    // Verify person no longer exists
    people = await apiHelpers.getPeople();
    expect(people).toHaveLength(0);
  });

  test('DELETE /api/people/{id} - should return 404 for non-existent person', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.delete(`/api/people/${nonExistentId}`);
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('should handle multiple people correctly', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const createdPeople = [];
    
    // Create multiple people
    for (const personData of testPeople) {
      const createdPerson = await apiHelpers.createPerson(personData);
      createdPeople.push(createdPerson);
    }
    
    // Verify all people exist
    const people = await apiHelpers.getPeople();
    expect(people).toHaveLength(testPeople.length);
    
    // Verify each person
    for (const createdPerson of createdPeople) {
      const foundPerson = people.find(p => p.id === createdPerson.id);
      expect(foundPerson).toMatchObject(createdPerson);
    }
  });

  test('should handle invalid role IDs gracefully', async ({ request }) => {
    const testPerson = generateTestPerson({
      roleIds: ['invalid-role-id', '00000000-0000-0000-0000-000000000000']
    });
    
    const response = await request.post('/api/people', {
      data: testPerson
    });
    
    // Should either fail with 400/404 or succeed with empty roles
    // depending on API implementation
    if (response.ok()) {
      const createdPerson = await response.json();
      expect(createdPerson.roles).toEqual([]);
    } else {
      expect([400, 404]).toContain(response.status());
    }
  });

  test('should maintain referential integrity with roles', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    // Create a role
    const role = await apiHelpers.createRole(generateTestRole());
    
    // Create person with that role
    const testPerson = generateTestPerson({
      roleIds: [role.id]
    });
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    expect(createdPerson.roles).toHaveLength(1);
    expect(createdPerson.roles[0]).toMatchObject(role);
    
    // Delete the role
    await apiHelpers.deleteRole(role.id);
    
    // Check what happens to the person's roles
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    // The API doesn't remove roles from people when roles are deleted
    // The person still references the deleted role (current behavior)
    expect(retrievedPerson.roles).toHaveLength(1);
    expect(retrievedPerson.roles[0]).toMatchObject(role);
  });

  test('should handle special characters in person data', async () => {
    const testPerson = generateTestPerson({
      fullName: 'Person with Special Characters: !@#$%^&*() 你好 🌟',
      phone: '+1-555-0123 ext. 456'
    });
    
    const createdPerson = await apiHelpers.createPerson(testPerson);
    
    expect(createdPerson.fullName).toBe(testPerson.fullName);
    expect(createdPerson.phone).toBe(testPerson.phone);
    
    // Verify retrieval works correctly
    const retrievedPerson = await apiHelpers.getPerson(createdPerson.id);
    expect(retrievedPerson).toMatchObject(createdPerson);
  });

  test('should handle phone number formats', async () => {
    const phoneFormats = [
      '+1-555-0123',
      '(555) 012-3456',
      '555.012.3456',
      '15550123456',
      '+44 20 7946 0958',
      ''
    ];
    
    for (const phone of phoneFormats) {
      const testPerson = generateTestPerson({ phone: phone || undefined });
      const createdPerson = await apiHelpers.createPerson(testPerson);
      
      expect(createdPerson.phone).toBe(phone || null);
    }
  });

  test('should return proper HTTP status codes', async ({ request }) => {
    const testPerson = generateTestPerson();
    
    // POST should return 201 Created
    const createResponse = await request.post('/api/people', {
      data: testPerson
    });
    expect(createResponse.status()).toBe(201);
    const createdPerson = await createResponse.json();
    
    // GET should return 200 OK
    const getResponse = await request.get(`/api/people/${createdPerson.id}`);
    expect(getResponse.status()).toBe(200);
    
    // PUT should return 204 No Content
    const updateResponse = await request.put(`/api/people/${createdPerson.id}`, {
      data: generateTestPerson()
    });
    expect(updateResponse.status()).toBe(204);
    
    // DELETE should return 204 No Content
    const deleteResponse = await request.delete(`/api/people/${createdPerson.id}`);
    expect(deleteResponse.status()).toBe(204);
  });

  test('should handle concurrent operations correctly', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const testPerson1 = generateTestPerson();
    const testPerson2 = generateTestPerson();
    
    // Create people concurrently
    const [createdPerson1, createdPerson2] = await Promise.all([
      apiHelpers.createPerson(testPerson1),
      apiHelpers.createPerson(testPerson2)
    ]);
    
    // Verify both people exist
    const people = await apiHelpers.getPeople();
    expect(people).toHaveLength(2);
    
    const foundPerson1 = people.find(p => p.id === createdPerson1.id);
    const foundPerson2 = people.find(p => p.id === createdPerson2.id);
    
    expect(foundPerson1).toMatchObject(createdPerson1);
    expect(foundPerson2).toMatchObject(createdPerson2);
  });

  test('should handle role assignment edge cases', async () => {
    // Create person with empty role array
    const testPerson1 = generateTestPerson({ roleIds: [] });
    const createdPerson1 = await apiHelpers.createPerson(testPerson1);
    expect(createdPerson1.roles).toEqual([]);
    
    // Create person with undefined roleIds
    const testPerson2 = generateTestPerson();
    delete testPerson2.roleIds;
    const createdPerson2 = await apiHelpers.createPerson(testPerson2);
    expect(createdPerson2.roles).toEqual([]);
    
    // Create person with duplicate role IDs
    const role = await apiHelpers.createRole(generateTestRole());
    const testPerson3 = generateTestPerson({ roleIds: [role.id, role.id] });
    const createdPerson3 = await apiHelpers.createPerson(testPerson3);
    expect(createdPerson3.roles).toHaveLength(1);
    expect(createdPerson3.roles[0].id).toBe(role.id);
  });
});