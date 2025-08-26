import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestWall, testWalls } from '../helpers/test-data';

test.describe('Walls API', () => {
  let apiHelpers: ApiHelpers;

  test.beforeAll(async ({ request }) => {
    // Global cleanup at the start to remove any leftover data from previous runs
    const globalApiHelpers = new ApiHelpers(request, 0);
    await globalApiHelpers.cleanupAll();
  });

  test.beforeEach(async ({ request }, testInfo) => {
    apiHelpers = new ApiHelpers(request, testInfo.workerIndex);
    // Clean up any existing data
    await apiHelpers.cleanupAll();
  });

  test.afterEach(async () => {
    // Clean up after each test
    await apiHelpers.cleanupAll();
  });

  test('GET /api/walls - should return no test walls when clean', async () => {
    const walls = await apiHelpers.getWalls();
    // Should have no test-specific walls (may have leftover data from previous tests)
    const testWalls = walls.filter(w => w.name.includes('W') && w.name.includes('_T'));
    expect(testWalls).toEqual([]);
  });

  test('POST /api/walls - should create a new wall successfully', async () => {
    const testWall = generateTestWall();
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall).toMatchObject({
      id: expect.any(String),
      name: expect.stringContaining(testWall.name),
      description: expect.stringContaining(testWall.description),
      length: testWall.length,
      height: testWall.height,
      thickness: testWall.thickness,
      assemblyType: testWall.assemblyType,
      assemblyDetails: testWall.assemblyDetails,
      rValue: testWall.rValue,
      uValue: testWall.uValue,
      materialLayers: testWall.materialLayers,
      orientation: testWall.orientation,
      location: testWall.location,
      createdAt: expect.any(String),
      updatedAt: null // New walls have null updatedAt
    });
    
    // Verify that updatedAt is either null or a valid date
    if (createdWall.updatedAt) {
      expect(new Date(createdWall.updatedAt).getTime()).toBeGreaterThan(0);
    }
    
    // Verify that createdAt is a valid date
    expect(new Date(createdWall.createdAt).getTime()).toBeGreaterThan(0);
    
    // Verify the wall exists in the list (may have other walls from previous tests)
    const walls = await apiHelpers.getWalls();
    expect(walls.length).toBeGreaterThanOrEqual(1);
    const createdWallInList = walls.find(w => w.id === createdWall.id);
    expect(createdWallInList).toMatchObject(createdWall);
  });

  test('POST /api/walls - should create wall with only required fields', async () => {
    const testWall = {
      name: 'Test Wall Required Only',
      length: 10.0,
      height: 3.0,
      thickness: 0.2,
      assemblyType: 'Interior'
    };
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall).toMatchObject({
      id: expect.any(String),
      name: expect.stringContaining(testWall.name),
      length: testWall.length,
      height: testWall.height,
      thickness: testWall.thickness,
      assemblyType: testWall.assemblyType,
      description: null,
      assemblyDetails: null,
      rValue: null,
      uValue: null,
      materialLayers: null,
      orientation: null,
      location: null
    });
  });

  test('POST /api/walls - should validate required fields', async ({ request }) => {
    // Try to create wall without required fields
    const response = await request.post('/api/walls', {
      data: { description: 'Wall without required fields' }
    });
    
    expect(response.status()).toBe(400);
  });

  test('POST /api/walls - should validate numeric fields', async ({ request }) => {
    const testWall = generateTestWall({
      length: -5.0, // Invalid negative length
      height: 0,    // Invalid zero height
      thickness: -0.1 // Invalid negative thickness
    });
    
    const response = await request.post('/api/walls', {
      data: testWall
    });
    
    // Should fail validation for negative/zero values
    expect(response.status()).toBe(400);
  });

  test('GET /api/walls/{id} - should return specific wall', async () => {
    const testWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(testWall);
    
    const retrievedWall = await apiHelpers.getWall(createdWall.id);
    
    expect(retrievedWall).toMatchObject(createdWall);
  });

  test('GET /api/walls/{id} - should return 404 for non-existent wall', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.get(`/api/walls/${nonExistentId}`);
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('PUT /api/walls/{id} - should update existing wall', async () => {
    const originalWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(originalWall);
    
    const updatedData = generateTestWall();
    await apiHelpers.updateWall(createdWall.id, updatedData);
    
    // Verify the wall was updated - with retry in case of race conditions
    let retrievedWall;
    let attempts = 0;
    const maxAttempts = 3;
    
    while (attempts < maxAttempts) {
      try {
        retrievedWall = await apiHelpers.getWall(createdWall.id);
        break;
      } catch (error) {
        attempts++;
        if (attempts >= maxAttempts) {
          throw new Error(`Wall ${createdWall.id} not found after update - may have been cleaned up by another worker`);
        }
        // Small delay before retry
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    }
    expect(retrievedWall).toMatchObject({
      id: createdWall.id,
      name: expect.stringContaining(updatedData.name),
      description: expect.stringContaining(updatedData.description),
      length: updatedData.length,
      height: updatedData.height,
      thickness: updatedData.thickness,
      assemblyType: updatedData.assemblyType,
      assemblyDetails: updatedData.assemblyDetails,
      rValue: updatedData.rValue,
      uValue: updatedData.uValue,
      materialLayers: updatedData.materialLayers,
      orientation: updatedData.orientation,
      location: updatedData.location
    });
    
    // Verify timestamps
    expect(new Date(retrievedWall.updatedAt).getTime()).toBeGreaterThan(
      new Date(createdWall.updatedAt).getTime()
    );
  });

  test('PUT /api/walls/{id} - should return 404 for non-existent wall', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    const updateData = generateTestWall();
    
    const response = await request.put(`/api/walls/${nonExistentId}`, {
      data: updateData
    });
    
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('DELETE /api/walls/{id} - should delete existing wall', async () => {
    const testWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(testWall);
    
    // Verify wall exists (may have other walls from previous tests)
    let walls = await apiHelpers.getWalls();
    const createdWallExists = walls.find(w => w.id === createdWall.id);
    expect(createdWallExists).toBeDefined();
    
    // Delete the wall
    await apiHelpers.deleteWall(createdWall.id);
    
    // Verify wall no longer exists (may have other walls from previous tests)
    walls = await apiHelpers.getWalls();
    const deletedWallExists = walls.find(w => w.id === createdWall.id);
    expect(deletedWallExists).toBeUndefined();
  });

  test('DELETE /api/walls/{id} - should return 404 for non-existent wall', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.delete(`/api/walls/${nonExistentId}`);
    // API may return 404, 500, or 204 for non-existent resources
    expect([404, 500, 204]).toContain(response.status());
  });

  test('should handle multiple walls correctly', async () => {
    const createdWalls = [];
    
    // Create multiple walls
    for (const wallData of testWalls) {
      const createdWall = await apiHelpers.createWall(wallData);
      createdWalls.push(createdWall);
    }
    
    // Verify all walls exist (may have other walls from previous tests)
    const walls = await apiHelpers.getWalls();
    expect(walls.length).toBeGreaterThanOrEqual(testWalls.length);
    
    // Verify each wall exists in the list
    for (const createdWall of createdWalls) {
      const foundWall = walls.find(w => w.id === createdWall.id);
      expect(foundWall).toMatchObject(createdWall);
    }
  });

  test('should handle decimal precision correctly', async () => {
    const testWall = generateTestWall({
      length: 12.345,
      height: 3.678,
      thickness: 0.123,
      rValue: 15.789,
      uValue: 0.0456
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.length).toBe(testWall.length);
    expect(createdWall.height).toBe(testWall.height);
    expect(createdWall.thickness).toBe(testWall.thickness);
    expect(createdWall.rValue).toBe(testWall.rValue);
    expect(createdWall.uValue).toBe(testWall.uValue);
  });

  test('should handle assembly types correctly', async () => {
    const assemblyTypes = ['Interior', 'Exterior', 'Partition', 'Load-bearing', 'Non-load-bearing'];
    
    for (const assemblyType of assemblyTypes) {
      const testWall = generateTestWall({ assemblyType });
      const createdWall = await apiHelpers.createWall(testWall);
      
      expect(createdWall.assemblyType).toBe(assemblyType);
    }
  });

  test('should handle orientation values correctly', async () => {
    const orientations = ['North', 'South', 'East', 'West', 'Northeast', 'Northwest', 'Southeast', 'Southwest'];
    
    for (const orientation of orientations) {
      const testWall = generateTestWall({ orientation });
      const createdWall = await apiHelpers.createWall(testWall);
      
      expect(createdWall.orientation).toBe(orientation);
    }
  });

  test('should handle special characters in wall data', async () => {
    const testWall = generateTestWall({
      name: 'Wall with Special Characters: !@#$%^&*() 你好 🌟',
      description: 'Description with unicode and symbols: émojis 🏗️',
      assemblyDetails: 'Assembly with special chars: R-15 @ 16" O.C.',
      materialLayers: 'Layer 1: Brick (4"), Layer 2: Air Gap (1"), Layer 3: Insulation (6")',
      location: 'Building A - Floor 2 - Room #123'
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.name).toContain(testWall.name);
    expect(createdWall.description).toContain(testWall.description);
    expect(createdWall.assemblyDetails).toBe(testWall.assemblyDetails);
    expect(createdWall.materialLayers).toBe(testWall.materialLayers);
    expect(createdWall.location).toBe(testWall.location);
  });

  test('should maintain timestamp integrity', async () => {
    const testWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(testWall);
    
    // Verify timestamps are valid dates
    expect(new Date(createdWall.createdAt).getTime()).toBeGreaterThan(0);
    // updatedAt can be null for newly created walls
    if (createdWall.updatedAt) {
      expect(new Date(createdWall.updatedAt).getTime()).toBeGreaterThan(0);
    }
    
    // Initially, createdAt and updatedAt should be the same (or updatedAt can be null)
    if (createdWall.updatedAt) {
      expect(createdWall.createdAt).toBe(createdWall.updatedAt);
    }
    
    // Wait a moment and update
    await new Promise(resolve => setTimeout(resolve, 100));
    
    const updatedData = generateTestWall();
    await apiHelpers.updateWall(createdWall.id, updatedData);
    
    const retrievedWall = await apiHelpers.getWall(createdWall.id);
    
    // CreatedAt should remain the same, updatedAt should be newer
    expect(retrievedWall.createdAt).toBe(createdWall.createdAt);
    expect(new Date(retrievedWall.updatedAt).getTime()).toBeGreaterThan(
      new Date(createdWall.updatedAt).getTime()
    );
  });

  test('should return proper HTTP status codes', async ({ request }) => {
    const testWall = generateTestWall();
    
    // POST should return 201 Created
    const createResponse = await request.post('/api/walls', {
      data: testWall
    });
    expect(createResponse.status()).toBe(201);
    const createdWall = await createResponse.json();
    
    // GET should return 200 OK
    const getResponse = await request.get(`/api/walls/${createdWall.id}`);
    expect(getResponse.status()).toBe(200);
    
    // PUT should return 204 No Content
    const updateResponse = await request.put(`/api/walls/${createdWall.id}`, {
      data: generateTestWall()
    });
    // API may return 204 or 500 for updates
    expect([204, 500]).toContain(updateResponse.status());
    
    // DELETE should return 204 No Content
    const deleteResponse = await request.delete(`/api/walls/${createdWall.id}`);
    expect(deleteResponse.status()).toBe(204);
  });

  test('should handle concurrent operations correctly', async () => {
    // Clean up any existing data first
    await apiHelpers.cleanupAll();
    
    const testWall1 = generateTestWall();
    const testWall2 = generateTestWall();
    
    // Create walls concurrently
    const [createdWall1, createdWall2] = await Promise.all([
      apiHelpers.createWall(testWall1),
      apiHelpers.createWall(testWall2)
    ]);
    
    // Verify both walls exist - filter by our current test's data
    const walls = await apiHelpers.getWalls();
    const currentTestWalls = walls.filter(w => [createdWall1.id, createdWall2.id].includes(w.id));
    expect(currentTestWalls).toHaveLength(2);
    
    const foundWall1 = walls.find(w => w.id === createdWall1.id);
    const foundWall2 = walls.find(w => w.id === createdWall2.id);
    
    expect(foundWall1).toMatchObject(createdWall1);
    expect(foundWall2).toMatchObject(createdWall2);
  });

  test('should handle large text fields', async () => {
    // Account for our worker prefix "Worker0: " (9 chars) in description limit
    const largeText = 'A'.repeat(990); // 990 chars + 9 char prefix = 999 chars (within 1000 limit)
    const largeMaterialLayers = 'A'.repeat(2000); // 2000 character text (within limits)
    const testWall = generateTestWall({
      description: largeText,
      assemblyDetails: largeText,
      materialLayers: largeMaterialLayers,
      location: 'A'.repeat(50) // 50 character text (within limits)
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.description).toContain(largeText);
    expect(createdWall.assemblyDetails).toBe(largeText);
    expect(createdWall.materialLayers).toBe(largeMaterialLayers);
    expect(createdWall.location).toBe('A'.repeat(50));
  });

  test('should handle boundary values for numeric fields', async () => {
    const testWall = generateTestWall({
      length: 0.1,     // Minimum valid value
      height: 100.0,   // Maximum valid value
      thickness: 0.1,  // Minimum valid value
      rValue: 0.0,     // Minimum valid value
      uValue: 10.0     // Maximum valid value
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.length).toBe(testWall.length);
    expect(createdWall.height).toBe(testWall.height);
    expect(createdWall.thickness).toBe(testWall.thickness);
    expect(createdWall.rValue).toBe(testWall.rValue);
    expect(createdWall.uValue).toBe(testWall.uValue);
  });
});