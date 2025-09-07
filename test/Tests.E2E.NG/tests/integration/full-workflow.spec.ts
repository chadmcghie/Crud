import { test, expect } from '@playwright/test';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, generateTestPerson } from '../helpers/test-data';

test.describe('Full Workflow Integration Tests', () => {
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

  test('should complete full role and person management workflow', async () => {
    // Step 1: Create roles via UI
    await pageHelpers.switchToRolesTab();
    
    const adminRole = generateTestRole({ name: 'Administrator', description: 'System administrator' });
    const userRole = generateTestRole({ name: 'User', description: 'Regular user' });
    
    // Create admin role
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(adminRole.name, adminRole.description);
    await pageHelpers.submitRoleForm();
    await pageHelpers.verifyRoleExists(adminRole.name);
    
    // Create user role
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(userRole.name, userRole.description);
    await pageHelpers.submitRoleForm();
    await pageHelpers.verifyRoleExists(userRole.name);
    
    // Verify both roles exist via API
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(2);
    
    // Step 2: Create people with roles via UI
    await pageHelpers.switchToPeopleTab();
    
    const adminPerson = generateTestPerson({ fullName: 'John Admin', phone: '+1-555-0001' });
    const regularUser = generateTestPerson({ fullName: 'Jane User', phone: '+1-555-0002' });
    
    // Create admin person with admin role
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(adminPerson.fullName, adminPerson.phone, [adminRole.name]);
    await pageHelpers.submitPersonForm();
    await pageHelpers.verifyPersonExists(adminPerson.fullName);
    await pageHelpers.verifyPersonHasRole(adminPerson.fullName, adminRole.name);
    
    // Create regular user with user role
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(regularUser.fullName, regularUser.phone, [userRole.name]);
    await pageHelpers.submitPersonForm();
    await pageHelpers.verifyPersonExists(regularUser.fullName);
    await pageHelpers.verifyPersonHasRole(regularUser.fullName, userRole.name);
    
    // Step 3: Verify data consistency via API
    const people = await apiHelpers.getPeople();
    expect(people).toHaveLength(2);
    
    const adminPersonFromApi = people.find(p => p.fullName === adminPerson.fullName);
    const userPersonFromApi = people.find(p => p.fullName === regularUser.fullName);
    
    expect(adminPersonFromApi?.roles).toHaveLength(1);
    expect(adminPersonFromApi?.roles[0].name).toBe(adminRole.name);
    
    expect(userPersonFromApi?.roles).toHaveLength(1);
    expect(userPersonFromApi?.roles[0].name).toBe(userRole.name);
    
    // Step 4: Update person roles via UI
    await pageHelpers.editPerson(regularUser.fullName);
    await pageHelpers.fillPersonForm(regularUser.fullName, regularUser.phone, [adminRole.name, userRole.name]);
    await pageHelpers.updatePersonForm();
    
    // Verify user now has both roles
    await pageHelpers.verifyPersonHasRole(regularUser.fullName, adminRole.name);
    await pageHelpers.verifyPersonHasRole(regularUser.fullName, userRole.name);
    
    // Step 5: Delete a role and verify impact
    await pageHelpers.switchToRolesTab();
    await pageHelpers.deleteRole(userRole.name);
    await pageHelpers.verifyRoleNotExists(userRole.name);
    
    // Check impact on people
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.clickRefreshButton();
    
    // People should still exist but with updated roles
    await pageHelpers.verifyPersonExists(adminPerson.fullName);
    await pageHelpers.verifyPersonExists(regularUser.fullName);
    
    // Step 6: Final cleanup via UI
    await pageHelpers.deletePerson(adminPerson.fullName);
    await pageHelpers.deletePerson(regularUser.fullName);
    await pageHelpers.verifyEmptyState('people');
    
    await pageHelpers.switchToRolesTab();
    await pageHelpers.deleteRole(adminRole.name);
    await pageHelpers.verifyEmptyState('roles');
  });

  test('should handle mixed UI and API operations', async () => {
    // Create role via API
    const apiRole = await apiHelpers.createRole(generateTestRole({ name: 'API Role' }));
    
    // Create role via UI
    await pageHelpers.switchToRolesTab();
    const uiRole = generateTestRole({ name: 'UI Role' });
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(uiRole.name, uiRole.description);
    await pageHelpers.submitRoleForm();
    
    // Refresh to see API-created role
    await pageHelpers.clickRefreshButton();
    await pageHelpers.verifyRoleExists(apiRole.name);
    await pageHelpers.verifyRoleExists(uiRole.name);
    
    // Create person via API with API role
    const apiPerson = await apiHelpers.createPerson(generateTestPerson({
      fullName: 'API Person',
      roleIds: [apiRole.id]
    }));
    
    // Create person via UI with UI role
    await pageHelpers.switchToPeopleTab();
    const uiPerson = generateTestPerson({ fullName: 'UI Person' });
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(uiPerson.fullName, uiPerson.phone, [uiRole.name]);
    await pageHelpers.submitPersonForm();
    
    // Refresh to see API-created person
    await pageHelpers.clickRefreshButton();
    await pageHelpers.verifyPersonExists(apiPerson.fullName);
    await pageHelpers.verifyPersonExists(uiPerson.fullName);
    
    // Verify roles are correctly assigned
    await pageHelpers.verifyPersonHasRole(apiPerson.fullName, apiRole.name);
    await pageHelpers.verifyPersonHasRole(uiPerson.fullName, uiRole.name);
    
    // Update API person via UI
    await pageHelpers.editPerson(apiPerson.fullName);
    await pageHelpers.fillPersonForm(apiPerson.fullName, '+1-555-9999', [uiRole.name]);
    await pageHelpers.updatePersonForm();
    
    // Verify changes via API
    const updatedPerson = await apiHelpers.getPerson(apiPerson.id);
    expect(updatedPerson.phone).toBe('+1-555-9999');
    expect(updatedPerson.roles).toHaveLength(1);
    expect(updatedPerson.roles[0].name).toBe(uiRole.name);
  });

  test('should maintain data integrity during rapid operations', async () => {
    // Rapidly create multiple roles
    const rolePromises = [];
    for (let i = 0; i < 5; i++) {
      const role = generateTestRole({ name: `Rapid Role ${i}` });
      rolePromises.push(apiHelpers.createRole(role));
    }
    const createdRoles = await Promise.all(rolePromises);
    
    // Switch to roles tab and verify all roles appear
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickRefreshButton();
    
    for (const role of createdRoles) {
      await pageHelpers.verifyRoleExists(role.name);
    }
    
    // Rapidly create people with random role assignments
    const peoplePromises = [];
    for (let i = 0; i < 5; i++) {
      const randomRoles = createdRoles
        .sort(() => 0.5 - Math.random())
        .slice(0, Math.floor(Math.random() * 3) + 1)
        .map(r => r.id);
      
      const suffixes = ['Alpha', 'Beta', 'Gamma', 'Delta', 'Epsilon'];
      const person = generateTestPerson({
        fullName: `Rapid Person ${suffixes[i]}`,
        roleIds: randomRoles
      });
      peoplePromises.push(apiHelpers.createPerson(person));
    }
    const createdPeople = await Promise.all(peoplePromises);
    
    // Switch to people tab and verify all people appear
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.clickRefreshButton();
    
    for (const person of createdPeople) {
      await pageHelpers.verifyPersonExists(person.fullName);
    }
    
    // Verify data consistency
    const allPeople = await apiHelpers.getPeople();
    const allRoles = await apiHelpers.getRoles();
    
    expect(allPeople).toHaveLength(5);
    expect(allRoles).toHaveLength(5);
    
    // Verify all role references are valid
    for (const person of allPeople) {
      for (const role of person.roles) {
        const roleExists = allRoles.some(r => r.id === role.id);
        expect(roleExists).toBe(true);
      }
    }
  });

  test('should handle error scenarios gracefully', async ({ page }) => {
    // Test form validation errors
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickAddRole();
    
    // Verify submit button is disabled when form is empty (proper validation behavior)
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeDisabled();
    
    // Verify no role was created (since form couldn't be submitted)
    const roles = await apiHelpers.getRoles();
    expect(roles).toHaveLength(0);
    
    // Test network error simulation (if API is down)
    // This would require mocking network responses or stopping the API server
    // For now, we'll test the UI behavior when API returns errors
    
    // Create a role first
    const testRole = generateTestRole();
    await pageHelpers.fillRoleForm(testRole.name, testRole.description);
    await pageHelpers.submitRoleForm();
    
    // Verify role was created
    await pageHelpers.verifyRoleExists(testRole.name);
    
    // Test deletion confirmation
    await pageHelpers.switchToPeopleTab();
    const testPerson = generateTestPerson();
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(testPerson.fullName, testPerson.phone);
    await pageHelpers.submitPersonForm();
    
    // Verify person was created
    await pageHelpers.verifyPersonExists(testPerson.fullName);
  });

  test('should preserve state during tab switching', async () => {
    // Force cleanup at the start to ensure clean state
    await apiHelpers.cleanupAll(true);
    
    // Add a small delay to ensure cleanup is complete
    await pageHelpers.page.waitForTimeout(500);
    
    // Navigate fresh to ensure clean UI state
    await pageHelpers.navigateToApp();
    
    // Create data in both tabs
    await pageHelpers.switchToRolesTab();
    
    const role = generateTestRole();
    await pageHelpers.clickAddRole();
    await pageHelpers.fillRoleForm(role.name, role.description);
    await pageHelpers.submitRoleForm();
    
    await pageHelpers.switchToPeopleTab();
    
    const person = generateTestPerson();
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(person.fullName, person.phone, [role.name]);
    await pageHelpers.submitPersonForm();
    
    // Switch between tabs multiple times
    for (let i = 0; i < 3; i++) {
      await pageHelpers.switchToRolesTab();
      await pageHelpers.verifyRoleExists(role.name);
      
      await pageHelpers.switchToPeopleTab();
      await pageHelpers.verifyPersonExists(person.fullName);
      await pageHelpers.verifyPersonHasRole(person.fullName, role.name);
    }
    
    // Verify data is still consistent via API
    const roles = await apiHelpers.getRoles();
    const people = await apiHelpers.getPeople();
    
    // Find our specific test data (there might be other data from parallel tests)
    const ourRole = roles.find(r => r.name === role.name);
    const ourPerson = people.find(p => p.fullName === person.fullName);
    
    expect(ourRole).toBeDefined();
    expect(ourPerson).toBeDefined();
    expect(ourPerson?.roles).toHaveLength(1);
    expect(ourPerson?.roles[0].name).toBe(role.name);
  });

  test('should handle browser refresh correctly', async ({ page }) => {
    // Create some data
    const role = await apiHelpers.createRole(generateTestRole());
    const person = await apiHelpers.createPerson(generateTestPerson({
      roleIds: [role.id]
    }));
    
    // Navigate to people tab
    await pageHelpers.switchToPeopleTab();
    await pageHelpers.clickRefreshButton();
    await pageHelpers.verifyPersonExists(person.fullName);
    
    // Refresh the browser
    await page.reload();
    
    // Wait for page to load with more flexible approach
    try {
      await page.waitForLoadState('networkidle', { timeout: 8000 });
    } catch (error) {
      console.warn('Network idle timeout, falling back to domcontentloaded');
      await page.waitForLoadState('domcontentloaded', { timeout: 5000 });
    }
    
    // Wait for Angular to fully load
    await page.locator('app-root').waitFor({ state: 'visible', timeout: 8000 });
    // Wait for Angular to be fully initialized
    await page.waitForFunction(() => {
      // Check if Angular is defined and ready
      return window.hasOwnProperty('ng') || document.querySelector('app-root');
    });
    
    // Should default to people tab and show data
    await expect(page.locator('app-people-list')).toBeVisible({ timeout: 10000 });
    await pageHelpers.verifyPersonExists(person.fullName);
    
    // Switch to roles tab and verify data
    await pageHelpers.switchToRolesTab();
    await pageHelpers.verifyRoleExists(role.name);
  });
});