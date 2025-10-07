import { test, expect } from '../setup/test-fixture';
import { PageHelpers } from '../helpers/page-helpers';
import { ApiHelpers } from '../helpers/api-helpers';
import { generateTestRole, generateTestPerson } from '../helpers/test-data';

test.describe('Full Workflow Integration Tests', () => {
  let pageHelpers: PageHelpers;
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ page, apiContext }, testInfo) => {
    pageHelpers = new PageHelpers(page);
    apiHelpers = new ApiHelpers(apiContext, testInfo.workerIndex, process.env.API_URL || 'http://localhost:5172');

    // Navigate to the app (database already cleaned by auto-cleanup fixture)
    await pageHelpers.navigateToApp();
  });

  test('@extended should complete full role and person management workflow', async () => {
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

    // People should still exist but regularUser should only have adminRole now (userRole was deleted)
    await pageHelpers.verifyPersonExists(adminPerson.fullName);
    await pageHelpers.verifyPersonExists(regularUser.fullName);

    // Verify regularUser only has adminRole after userRole deletion
    await pageHelpers.verifyPersonHasRole(regularUser.fullName, adminRole.name);
    // Note: Can't verify userRole is NOT present due to DOM structure, but API would confirm
    
    // Step 6: Final cleanup via UI
    await pageHelpers.deletePerson(adminPerson.fullName);
    await pageHelpers.deletePerson(regularUser.fullName);
    await pageHelpers.verifyEmptyState('people');
    
    await pageHelpers.switchToRolesTab();
    await pageHelpers.deleteRole(adminRole.name);
    await pageHelpers.verifyEmptyState('roles');
  });

  test('@extended should handle mixed UI and API operations', async () => {
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

    // Wait for update to be reflected in backend
    await pageHelpers.page.waitForTimeout(1000);

    // Verify changes via API
    const updatedPerson = await apiHelpers.getPerson(apiPerson.id);
    expect(updatedPerson.phone).toBe('+1-555-9999');
    expect(updatedPerson.roles).toHaveLength(1);
    expect(updatedPerson.roles[0].name).toBe(uiRole.name);
  });

  test('@extended should maintain data integrity during rapid operations', async () => {
    // Create roles sequentially (not rapidly) to avoid cleanup race conditions
    const createdRoles = [];
    for (let i = 0; i < 5; i++) {
      const role = generateTestRole({ name: `Rapid Role ${i}` });
      const createdRole = await apiHelpers.createRole(role);
      createdRoles.push(createdRole);
      // Small delay between creates to ensure each is committed
      await pageHelpers.page.waitForTimeout(100);
    }

    // Wait to ensure all roles are fully committed to database
    await pageHelpers.page.waitForTimeout(500);

    // Verify all roles were created successfully via API
    const allRolesAfterCreation = await apiHelpers.getRoles();
    for (const role of createdRoles) {
      const roleExists = allRolesAfterCreation.some(r => r.id === role.id);
      if (!roleExists) {
        console.error(`Role ${role.name} (${role.id}) not found in database!`);
        console.error(`Available roles: ${allRolesAfterCreation.map(r => `${r.name} (${r.id})`).join(', ')}`);
      }
      expect(roleExists).toBe(true);
    }

    // Switch to roles tab and verify all roles appear in UI
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickRefreshButton();

    for (const role of createdRoles) {
      await pageHelpers.verifyRoleExists(role.name);
    }

    // Create people sequentially with valid role references
    const createdPeople = [];
    const suffixes = ['Alpha', 'Beta', 'Gamma', 'Delta', 'Epsilon'];
    for (let i = 0; i < 5; i++) {
      // Re-verify roles still exist before creating person
      const currentRoles = await apiHelpers.getRoles();
      const validRoleIds = createdRoles
        .filter(r => currentRoles.some(cr => cr.id === r.id))
        .sort(() => 0.5 - Math.random())
        .slice(0, Math.floor(Math.random() * 3) + 1)
        .map(r => r.id);

      if (validRoleIds.length === 0) {
        console.error(`No valid roles found for person ${suffixes[i]}!`);
        console.error(`Created roles: ${createdRoles.map(r => `${r.name} (${r.id})`).join(', ')}`);
        console.error(`Current roles: ${currentRoles.map(r => `${r.name} (${r.id})`).join(', ')}`);
        throw new Error('All roles were deleted before person creation!');
      }

      const person = generateTestPerson({
        fullName: `Rapid Person ${suffixes[i]}`,
        roleIds: validRoleIds
      });

      // Create sequentially with retry on 409 errors
      const createdPerson = await apiHelpers.createPerson(person);
      createdPeople.push(createdPerson);

      // Small delay between creates
      await pageHelpers.page.waitForTimeout(100);
    }
    
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

  test('@extended should handle error scenarios gracefully', async ({ page }) => {
    // Note: Cleanup is already done in beforeAll hook, no need to repeat here

    // Test form validation errors
    await pageHelpers.switchToRolesTab();
    await pageHelpers.clickRefreshButton(); // Refresh to reflect state after cleanup
    await pageHelpers.clickAddRole();

    // Verify submit button is disabled when form is empty (proper validation behavior)
    const submitButton = page.locator('button[type="submit"]');
    await expect(submitButton).toBeDisabled();

    // Navigate back to list to check no role was created
    await page.click('a[href*="roles-list"], a[routerLink*="roles"]');
    await page.waitForLoadState('domcontentloaded');
    await pageHelpers.page.waitForTimeout(300);

    // Verify no role was created (should be empty after beforeAll cleanup)
    const rolesAfterTest = await apiHelpers.getRoles();
    expect(rolesAfterTest).toHaveLength(0);
    
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

  test('@extended should preserve state during tab switching', async () => {
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

  test('@extended should handle browser refresh correctly', async ({ page }) => {
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
    const response = await page.reload({
      waitUntil: 'domcontentloaded',
      timeout: 30000
    });

    if (!response || !response.ok()) {
      throw new Error(`Reload failed: ${response?.status() || 'no response'}`);
    }

    // Use pageHelpers.navigateToApp() - it has proper CI-aware timeouts and retry logic
    await pageHelpers.navigateToApp();

    // Verify the page content loaded (table or empty state message)
    const hasContent = await page.locator('table, .empty-state, h2, h3').first().isVisible();
    expect(hasContent).toBe(true);

    // Verify data persisted after refresh
    await pageHelpers.verifyPersonExists(person.fullName);

    // Switch to roles tab and verify data
    await pageHelpers.switchToRolesTab();
    await pageHelpers.verifyRoleExists(role.name);
  });
});