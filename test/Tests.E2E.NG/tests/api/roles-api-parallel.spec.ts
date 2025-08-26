// Example test using the improved worker isolation fixture
// This test demonstrates worker isolation with shared API server and database reset

import { test, expect } from '../fixtures/worker-isolation-improved';
import { generateTestRole } from '../helpers/test-data';

test.describe('Roles API - Parallel Execution', () => {
  
  test('should create role in isolated worker environment', async ({ isolatedApiHelpers, workerIndex }) => {
    console.log(`🧪 Running test in worker ${workerIndex}`);
    
    // Generate unique test data for this worker
    const testRole = generateTestRole({ 
      name: `ParallelTest_W${workerIndex}_${Date.now()}` 
    }, workerIndex);
    
    // Create role
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    expect(createdRole.name).toBe(testRole.name);
    expect(createdRole.description).toBe(testRole.description);
    
    // Verify it exists
    const roles = await isolatedApiHelpers.getRoles();
    const foundRole = roles.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
    expect(foundRole!.name).toBe(testRole.name);
  });

  test('should handle concurrent role creation without interference', async ({ isolatedApiHelpers, workerIndex }) => {
    console.log(`🧪 Running concurrent test in worker ${workerIndex}`);
    
    // Create multiple roles concurrently within this worker
    const rolePromises = Array.from({ length: 3 }, (_, i) => {
      const testRole = generateTestRole({ 
        name: `ConcurrentRole_W${workerIndex}_${i}_${Date.now()}` 
      }, workerIndex);
      return isolatedApiHelpers.createRole(testRole);
    });
    
    const createdRoles = await Promise.all(rolePromises);
    
    // Verify all roles were created successfully
    expect(createdRoles).toHaveLength(3);
    createdRoles.forEach((role, index) => {
      expect(role.name).toContain(`ConcurrentRole_W${workerIndex}_${index}`);
    });
    
    // Verify they all exist in the database
    const allRoles = await isolatedApiHelpers.getRoles();
    createdRoles.forEach(createdRole => {
      const foundRole = allRoles.find(r => r.id === createdRole.id);
      expect(foundRole).toBeDefined();
    });
  });

  test('should have clean database state for each test', async ({ isolatedApiHelpers, databaseRespawn }) => {
    console.log(`🧪 Testing clean state in worker ${databaseRespawn.getWorkerIndex()}`);
    
    // Check that we start with a clean database
    const initialRoles = await isolatedApiHelpers.getRoles();
    const testRoles = initialRoles.filter(r => r.name.includes('Test') || r.name.includes('Parallel'));
    expect(testRoles).toHaveLength(0); // Should be no test data from previous tests
    
    // Create a test role
    const workerIndex = databaseRespawn.getWorkerIndex();
    const testRole = generateTestRole({ 
      name: `CleanStateTest_${Date.now()}` 
    }, workerIndex);
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    
    // Verify it was created
    const rolesAfterCreate = await isolatedApiHelpers.getRoles();
    const foundRole = rolesAfterCreate.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
    
    // Note: The next test will start with a clean database due to the fixture
  });

  test('should verify database isolation between workers', async ({ isolatedApiHelpers, workerIndex }) => {
    console.log(`🧪 Testing isolation in worker ${workerIndex}`);
    
    // Create a role with worker-specific name
    const testRole = generateTestRole({ 
      name: `Worker${workerIndex}_IsolationTest_${Date.now()}` 
    }, workerIndex);
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    
    // Verify the role exists
    const roles = await isolatedApiHelpers.getRoles();
    const foundRole = roles.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
    expect(foundRole!.name).toContain(`Worker${workerIndex}_`);
    
    // Note: With database reset, we shouldn't see roles from other workers
    // since each worker starts with a clean database
    const otherWorkerRoles = roles.filter(r => 
      r.name.includes('Worker') && 
      !r.name.includes(`Worker${workerIndex}_`)
    );
    expect(otherWorkerRoles).toHaveLength(0);
  });
});
