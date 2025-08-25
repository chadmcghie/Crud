import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestWall, testWalls } from '../helpers/test-data';

test.describe('Walls API', () => {
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

  test('GET /api/walls - should return empty array when no walls exist', async () => {
    const walls = await apiHelpers.getWalls();
    expect(walls).toEqual([]);
  });

  test('POST /api/walls - should create a new wall successfully', async () => {
    const testWall = generateTestWall();
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall).toMatchObject({
      id: expect.any(String),
      name: testWall.name,
      description: testWall.description,
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
      updatedAt: expect.any(String)
    });
    
    // Verify the wall exists in the list
    const walls = await apiHelpers.getWalls();
    expect(walls).toHaveLength(1);
    expect(walls[0]).toMatchObject(createdWall);
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
      name: testWall.name,
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
    expect(response.status()).toBe(404);
  });

  test('PUT /api/walls/{id} - should update existing wall', async () => {
    const originalWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(originalWall);
    
    const updatedData = generateTestWall();
    await apiHelpers.updateWall(createdWall.id, updatedData);
    
    // Verify the wall was updated
    const retrievedWall = await apiHelpers.getWall(createdWall.id);
    expect(retrievedWall).toMatchObject({
      id: createdWall.id,
      name: updatedData.name,
      description: updatedData.description,
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
    
    expect(response.status()).toBe(404);
  });

  test('DELETE /api/walls/{id} - should delete existing wall', async () => {
    const testWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(testWall);
    
    // Verify wall exists
    let walls = await apiHelpers.getWalls();
    expect(walls).toHaveLength(1);
    
    // Delete the wall
    await apiHelpers.deleteWall(createdWall.id);
    
    // Verify wall no longer exists
    walls = await apiHelpers.getWalls();
    expect(walls).toHaveLength(0);
  });

  test('DELETE /api/walls/{id} - should return 404 for non-existent wall', async ({ request }) => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    
    const response = await request.delete(`/api/walls/${nonExistentId}`);
    expect(response.status()).toBe(404);
  });

  test('should handle multiple walls correctly', async () => {
    const createdWalls = [];
    
    // Create multiple walls
    for (const wallData of testWalls) {
      const createdWall = await apiHelpers.createWall(wallData);
      createdWalls.push(createdWall);
    }
    
    // Verify all walls exist
    const walls = await apiHelpers.getWalls();
    expect(walls).toHaveLength(testWalls.length);
    
    // Verify each wall
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
    
    expect(createdWall.name).toBe(testWall.name);
    expect(createdWall.description).toBe(testWall.description);
    expect(createdWall.assemblyDetails).toBe(testWall.assemblyDetails);
    expect(createdWall.materialLayers).toBe(testWall.materialLayers);
    expect(createdWall.location).toBe(testWall.location);
  });

  test('should maintain timestamp integrity', async () => {
    const testWall = generateTestWall();
    const createdWall = await apiHelpers.createWall(testWall);
    
    // Verify timestamps are valid dates
    expect(new Date(createdWall.createdAt).getTime()).toBeGreaterThan(0);
    expect(new Date(createdWall.updatedAt).getTime()).toBeGreaterThan(0);
    
    // Initially, createdAt and updatedAt should be the same
    expect(createdWall.createdAt).toBe(createdWall.updatedAt);
    
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
    expect(updateResponse.status()).toBe(204);
    
    // DELETE should return 204 No Content
    const deleteResponse = await request.delete(`/api/walls/${createdWall.id}`);
    expect(deleteResponse.status()).toBe(204);
  });

  test('should handle concurrent operations correctly', async () => {
    const testWall1 = generateTestWall();
    const testWall2 = generateTestWall();
    
    // Create walls concurrently
    const [createdWall1, createdWall2] = await Promise.all([
      apiHelpers.createWall(testWall1),
      apiHelpers.createWall(testWall2)
    ]);
    
    // Verify both walls exist
    const walls = await apiHelpers.getWalls();
    expect(walls).toHaveLength(2);
    
    const foundWall1 = walls.find(w => w.id === createdWall1.id);
    const foundWall2 = walls.find(w => w.id === createdWall2.id);
    
    expect(foundWall1).toMatchObject(createdWall1);
    expect(foundWall2).toMatchObject(createdWall2);
  });

  test('should handle large text fields', async () => {
    const largeText = 'A'.repeat(2000); // 2000 character text
    const testWall = generateTestWall({
      description: largeText,
      assemblyDetails: largeText,
      materialLayers: largeText,
      location: largeText
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.description).toBe(largeText);
    expect(createdWall.assemblyDetails).toBe(largeText);
    expect(createdWall.materialLayers).toBe(largeText);
    expect(createdWall.location).toBe(largeText);
  });

  test('should handle boundary values for numeric fields', async () => {
    const testWall = generateTestWall({
      length: 0.01,    // Very small positive value
      height: 100.0,   // Large value
      thickness: 0.001, // Very small thickness
      rValue: 0.0,     // Zero R-value
      uValue: 999.999  // Large U-value
    });
    
    const createdWall = await apiHelpers.createWall(testWall);
    
    expect(createdWall.length).toBe(testWall.length);
    expect(createdWall.height).toBe(testWall.height);
    expect(createdWall.thickness).toBe(testWall.thickness);
    expect(createdWall.rValue).toBe(testWall.rValue);
    expect(createdWall.uValue).toBe(testWall.uValue);
  });
});