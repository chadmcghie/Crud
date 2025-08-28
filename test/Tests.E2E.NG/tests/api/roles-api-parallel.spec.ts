// Example test using the improved worker isolation fixture
// This test demonstrates worker isolation with shared API server and database reset

import { test, expect } from '../fixtures/worker-isolation-improved';
import { generateTestRole } from '../helpers/test-data';

test.describe('Roles API - Parallel Execution', () => {
  
  test('should create role in isolated worker environment', async ({ isolatedApiHelpers }) => {
    console.log('🧪 Running test in serial execution mode');
    
    // Generate unique test data for serial execution
    const testRole = generateTestRole({ 
      name: `SerialTest_${Date.now()}` 
    }, 0);
    
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

  test('should handle concurrent role creation without interference', async ({ isolatedApiHelpers }) => {
    console.log('🧪 Running concurrent test in serial execution mode');
    
    // Create multiple roles concurrently in serial execution
    const rolePromises = Array.from({ length: 3 }, (_, i) => {
      const testRole = generateTestRole({ 
        name: `ConcurrentRole_${i}_${Date.now()}` 
      }, 0);
      return isolatedApiHelpers.createRole(testRole);
    });
    
    const createdRoles = await Promise.all(rolePromises);
    
    // Verify all roles were created successfully
    expect(createdRoles).toHaveLength(3);
    createdRoles.forEach((role, index) => {
      expect(role.name).toContain(`ConcurrentRole_${index}`);
    });
    
    // Verify they all exist in the database
    const allRoles = await isolatedApiHelpers.getRoles();
    createdRoles.forEach(createdRole => {
      const foundRole = allRoles.find(r => r.id === createdRole.id);
      expect(foundRole).toBeDefined();
    });
  });

  test('should have clean database state for each test', async ({ isolatedApiHelpers, databaseRespawn }) => {
    console.log('🧪 Testing clean state in serial execution mode');
    
    // Check that we start with a clean database
    const initialRoles = await isolatedApiHelpers.getRoles();
    const testRoles = initialRoles.filter(r => r.name.includes('Test') || r.name.includes('Parallel'));
    expect(testRoles).toHaveLength(0); // Should be no test data from previous tests
    
    // Create a test role
    const testRole = generateTestRole({ 
      name: `CleanStateTest_${Date.now()}` 
    }, 0);
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    
    // Verify it was created
    const rolesAfterCreate = await isolatedApiHelpers.getRoles();
    const foundRole = rolesAfterCreate.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
    
    // Note: The next test will start with a clean database due to the fixture
  });

  test('should verify database isolation in serial execution', async ({ isolatedApiHelpers }) => {
    console.log('🧪 Testing database state in serial execution mode');
    
    // Create a role with serial-specific name
    const testRole = generateTestRole({ 
      name: `Serial_IsolationTest_${Date.now()}` 
    }, 0);
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    
    // Verify the role exists
    const roles = await isolatedApiHelpers.getRoles();
    const foundRole = roles.find(r => r.id === createdRole.id);
    expect(foundRole).toBeDefined();
    expect(foundRole!.name).toContain('Serial_');
    
    // Note: In serial execution, we have a clean database for each test
    // so we shouldn't see roles from other test runs
    const otherTestRoles = roles.filter(r => 
      r.name.includes('Worker') && 
      !r.name.includes('Serial_')
    );
    expect(otherTestRoles).toHaveLength(0);
  });
});
