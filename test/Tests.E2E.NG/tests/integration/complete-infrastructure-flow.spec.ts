import { test, expect } from '@playwright/test';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, generateTestPerson } from '../helpers/test-data';

test.describe('Complete Infrastructure Flow Integration Tests', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, request }, testInfo) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(request, 0);
    
    // Force immediate cleanup for UI tests to ensure complete isolation
    await apiHelpers.cleanupAll(true);
    
    // Navigate to the app
    await pageHelpers.navigateToApp();
  });

  test.afterEach(async () => {
    // Force immediate cleanup after each test
    await apiHelpers.cleanupAll(true);
  });

  test('should validate complete E2E infrastructure flow', async () => {
    // This test validates the complete infrastructure flow:
    // 1. Server startup (handled by global-setup.ts)
    // 2. Database initialization and cleanup
    // 3. Test execution with proper isolation
    // 4. Data persistence and retrieval
    // 5. Clean teardown (handled by global-teardown.ts)

    // Step 1: Verify servers are running and accessible
    const healthCheck = await apiHelpers.request.get('/health');
    expect(healthCheck.status()).toBe(200);
    
    // Step 2: Verify database is clean and ready
    const initialRoles = await apiHelpers.getRoles();
    const initialPeople = await apiHelpers.getPeople();
    expect(initialRoles).toHaveLength(0);
    expect(initialPeople).toHaveLength(0);
    
    // Step 3: Create test data to verify database operations
    const testRole = generateTestRole({ name: 'Infrastructure Test Role' });
    const createdRole = await apiHelpers.createRole(testRole);
    expect(createdRole.id).toBeDefined();
    expect(createdRole.name).toBe(testRole.name);
    
    const testPerson = generateTestPerson({ 
      fullName: 'Infrastructure Test Person',
      roleIds: [createdRole.id]
    });
    const createdPerson = await apiHelpers.createPerson(testPerson);
    expect(createdPerson.id).toBeDefined();
    expect(createdPerson.fullName).toBe(testPerson.fullName);
    expect(createdPerson.roles).toHaveLength(1);
    expect(createdPerson.roles[0].id).toBe(createdRole.id);
    
    // Step 4: Verify UI can access and display the data
    await pageHelpers.switchToRolesTab();
    await pageHelpers.verifyRoleExists(testRole.name);
    
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.verifyPersonExists(testPerson.fullName);
    await pageHelpers.verifyPersonHasRole(testPerson.fullName, testRole.name);
    
    // Step 5: Verify data consistency across operations
    const roles = await apiHelpers.getRoles();
    const people = await apiHelpers.getPeople();
    
    expect(roles).toHaveLength(1);
    expect(people).toHaveLength(1);
    
    const retrievedRole = roles.find(r => r.id === createdRole.id);
    const retrievedPerson = people.find(p => p.id === createdPerson.id);
    
    expect(retrievedRole).toBeDefined();
    expect(retrievedPerson).toBeDefined();
    expect(retrievedPerson?.roles[0].id).toBe(retrievedRole?.id);
    
    // Step 6: Test cleanup operations
    await apiHelpers.deletePerson(createdPerson.id);
    await apiHelpers.deleteRole(createdRole.id);
    
    const finalRoles = await apiHelpers.getRoles();
    const finalPeople = await apiHelpers.getPeople();
    
    expect(finalRoles).toHaveLength(0);
    expect(finalPeople).toHaveLength(0);
  });

  test('should validate database isolation between test files', async () => {
    // This test ensures that database cleanup between test files works correctly
    // and that each test file starts with a clean database state
    
    // Create some test data
    const role1 = await apiHelpers.createRole(generateTestRole({ name: 'Isolation Test Role 1' }));
    const role2 = await apiHelpers.createRole(generateTestRole({ name: 'Isolation Test Role 2' }));
    
    const person1 = await apiHelpers.createPerson(generateTestPerson({
      fullName: 'Isolation Test Person 1',
      roleIds: [role1.id]
    }));
    
    const person2 = await apiHelpers.createPerson(generateTestPerson({
      fullName: 'Isolation Test Person 2',
      roleIds: [role2.id]
    }));
    
    // Verify data exists
    const roles = await apiHelpers.getRoles();
    const people = await apiHelpers.getPeople();
    
    expect(roles).toHaveLength(2);
    expect(people).toHaveLength(2);
    
    // Verify UI can display the data
    await pageHelpers.switchToRolesTab();
    await pageHelpers.verifyRoleExists(role1.name);
    await pageHelpers.verifyRoleExists(role2.name);
    
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.verifyPersonExists(person1.fullName);
    await pageHelpers.verifyPersonExists(person2.fullName);
    
    // This test will be cleaned up by the afterEach hook
    // The next test file should start with a completely clean database
  });

  test('should validate server management and health checks', async () => {
    // This test validates that the server management infrastructure is working correctly
    
    // Test API server health
    const apiHealth = await apiHelpers.request.get('/health');
    expect(apiHealth.status()).toBe(200);
    
    // Test API endpoints are accessible
    const rolesResponse = await apiHelpers.request.get('/api/roles');
    expect(rolesResponse.status()).toBe(200);
    
    const peopleResponse = await apiHelpers.request.get('/api/people');
    expect(peopleResponse.status()).toBe(200);
    
    // Test Angular frontend is accessible
    await pageHelpers.navigateToApp();
    await expect(pageHelpers.page.locator('app-root')).toBeVisible();
    
    // Test navigation between tabs works
    await pageHelpers.switchToRolesTab();
    await expect(pageHelpers.page.locator('app-roles-list')).toBeVisible();
    
    await pageHelpers.switchToPeopleTab();
    await expect(pageHelpers.page.locator('app-people-list')).toBeVisible();
    
    // Test that we can perform basic operations
    const testRole = generateTestRole({ name: 'Server Health Test Role' });
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    await pageHelpers.submitRoleForm();
    await pageHelpers.verifyRoleExists(testRole.name);
  });

  test('should validate performance meets targets', async () => {
    // This test validates that the infrastructure meets performance targets:
    // - Server startup: < 30 seconds (handled by global-setup)
    // - Database reset: < 1 second per file
    // - Test execution: reasonable performance
    
    const startTime = Date.now();
    
    // Test database operations performance
    const rolePromises = [];
    for (let i = 0; i < 10; i++) {
      const role = generateTestRole({ name: `Performance Test Role ${i}` });
      rolePromises.push(apiHelpers.createRole(role));
    }
    
    const createdRoles = await Promise.all(rolePromises);
    const createTime = Date.now() - startTime;
    
    // Creating 10 roles should be reasonably fast (less than 5 seconds)
    expect(createTime).toBeLessThan(5000);
    
    // Test UI performance
    const uiStartTime = Date.now();
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickRefreshButton();
    
    // Verify all roles are visible
    for (const role of createdRoles) {
      await pageHelpers.verifyRoleExists(role.name);
    }
    
    const uiTime = Date.now() - uiStartTime;
    
    // UI operations should be reasonably fast (less than 10 seconds for 10 items)
    expect(uiTime).toBeLessThan(10000);
    
    // Test cleanup performance
    const cleanupStartTime = Date.now();
    await apiHelpers.cleanupAll(true);
    const cleanupTime = Date.now() - cleanupStartTime;
    
    // Cleanup should be very fast (less than 2 seconds)
    expect(cleanupTime).toBeLessThan(2000);
    
    // Verify cleanup worked
    const finalRoles = await apiHelpers.getRoles();
    expect(finalRoles).toHaveLength(0);
  });

  test('should validate error handling and recovery', async () => {
    // This test validates that the infrastructure handles errors gracefully
    
    // Test invalid API requests
    const invalidRoleResponse = await apiHelpers.request.post('/api/roles', {
      data: { name: '', description: '' } // Invalid data
    });
    expect(invalidRoleResponse.status()).toBe(400);
    
    // Test non-existent resource access
    const nonExistentRole = await apiHelpers.request.get('/api/roles/99999');
    expect(nonExistentRole.status()).toBe(404);
    
    // Test UI error handling
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickAddRole();
    
    // Try to submit empty form (should be prevented by validation)
    const submitButton = pageHelpers.page.locator('button[type="submit"]');
    await expect(submitButton).toBeDisabled();
    
    // Fill form with valid data
    const testRole = generateTestRole({ name: 'Error Handling Test Role' });
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    await pageHelpers.submitRoleForm();
    await pageHelpers.verifyRoleExists(testRole.name);
    
    // Test that the system recovers and continues working
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(1);
    expect(roles[0].name).toBe(testRole.name);
  });
});
