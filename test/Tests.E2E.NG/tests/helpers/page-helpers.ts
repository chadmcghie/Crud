// Page object helpers for Angular UI interactions
import { Page, Locator, expect } from '@playwright/test';

export class PageHelpers {
  constructor(private page: Page) {}

  // Navigation helpers
  async navigateToApp(): Promise<void> {
    await this.page.goto('/');
    await this.page.waitForLoadState('networkidle');
  }

  async switchToPeopleTab(): Promise<void> {
    await this.page.click('button:has-text("👥 People Management")');
    await this.page.waitForSelector('app-people-list');
  }

  async switchToRolesTab(): Promise<void> {
    await this.page.click('button:has-text("🎭 Roles Management")');
    await this.page.waitForSelector('app-roles-list');
  }

  // Role management helpers
  async clickAddRole(): Promise<void> {
    await this.page.click('button:has-text("Add New Role")');
    await this.page.waitForSelector('app-roles form');
  }

  async fillRoleForm(name: string, description?: string): Promise<void> {
    await this.page.fill('input#name', name);
    if (description) {
      await this.page.fill('textarea#description', description);
    }
  }

  async submitRoleForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Create Role")');
    await this.page.waitForSelector('app-roles form', { state: 'hidden' });
  }

  async editRole(roleName: string): Promise<void> {
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`);
    await roleRow.locator('button:has-text("Edit")').click();
    await this.page.waitForSelector('app-roles form');
  }

  async updateRoleForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Update Role")');
    await this.page.waitForSelector('app-roles form', { state: 'hidden' });
  }

  async deleteRole(roleName: string): Promise<void> {
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`);
    
    // Handle the confirmation dialog
    this.page.on('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await roleRow.locator('button:has-text("Delete")').click();
    await this.page.waitForTimeout(500); // Wait for deletion to complete
  }

  async getRoleRowCount(): Promise<number> {
    const rows = await this.page.locator('.roles-table tbody tr').count();
    return rows;
  }

  async verifyRoleExists(roleName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${roleName}")`)).toBeVisible();
  }

  async verifyRoleNotExists(roleName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${roleName}")`)).not.toBeVisible();
  }

  // Person management helpers
  async clickAddPerson(): Promise<void> {
    await this.page.click('button:has-text("Add New Person")');
    await this.page.waitForSelector('app-people form');
  }

  async fillPersonForm(fullName: string, phone?: string, roleNames?: string[]): Promise<void> {
    await this.page.fill('input#fullName', fullName);
    if (phone) {
      await this.page.fill('input#phone', phone);
    }
    
    if (roleNames && roleNames.length > 0) {
      // First, uncheck all roles
      const checkboxes = await this.page.locator('input[type="checkbox"]').all();
      for (const checkbox of checkboxes) {
        if (await checkbox.isChecked()) {
          await checkbox.uncheck();
        }
      }
      
      // Then check the specified roles
      for (const roleName of roleNames) {
        const roleCheckbox = this.page.locator(`input[type="checkbox"][id*="role"]:near(label:has-text("${roleName}"))`);
        await roleCheckbox.check();
      }
    }
  }

  async submitPersonForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Create Person")');
    await this.page.waitForSelector('app-people form', { state: 'hidden' });
  }

  async editPerson(personName: string): Promise<void> {
    const personRow = this.page.locator(`tr:has-text("${personName}")`);
    await personRow.locator('button:has-text("Edit")').click();
    await this.page.waitForSelector('app-people form');
  }

  async updatePersonForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Update Person")');
    await this.page.waitForSelector('app-people form', { state: 'hidden' });
  }

  async deletePerson(personName: string): Promise<void> {
    const personRow = this.page.locator(`tr:has-text("${personName}")`);
    
    // Handle the confirmation dialog
    this.page.on('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await personRow.locator('button:has-text("Delete")').click();
    await this.page.waitForTimeout(500); // Wait for deletion to complete
  }

  async getPersonRowCount(): Promise<number> {
    const rows = await this.page.locator('.people-table tbody tr').count();
    return rows;
  }

  async verifyPersonExists(personName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${personName}")`)).toBeVisible();
  }

  async verifyPersonNotExists(personName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${personName}")`)).not.toBeVisible();
  }

  async verifyPersonHasRole(personName: string, roleName: string): Promise<void> {
    const personRow = this.page.locator(`tr:has-text("${personName}")`);
    await expect(personRow.locator(`.roles-cell:has-text("${roleName}")`)).toBeVisible();
  }

  // Form validation helpers
  async verifyFormValidationError(fieldName: string, expectedError: string): Promise<void> {
    const errorMessage = this.page.locator(`.error-message:near(input#${fieldName})`);
    await expect(errorMessage).toContainText(expectedError);
  }

  async verifySubmitButtonDisabled(): Promise<void> {
    await expect(this.page.locator('button[type="submit"]')).toBeDisabled();
  }

  async verifySubmitButtonEnabled(): Promise<void> {
    await expect(this.page.locator('button[type="submit"]')).toBeEnabled();
  }

  // General helpers
  async refreshPage(): Promise<void> {
    await this.page.reload();
    await this.page.waitForLoadState('networkidle');
  }

  async clickRefreshButton(): Promise<void> {
    await this.page.click('button:has-text("Refresh")');
    await this.page.waitForTimeout(500);
  }

  async verifyEmptyState(entityType: 'roles' | 'people'): Promise<void> {
    const emptyStateText = entityType === 'roles' 
      ? 'No roles found. Add the first role'
      : 'No people found. Add the first person';
    await expect(this.page.locator('.empty-state')).toContainText(emptyStateText);
  }

  async verifyPageTitle(): Promise<void> {
    await expect(this.page.locator('h1')).toContainText('People & Roles Management System');
  }

  async verifyTabActive(tabName: 'people' | 'roles'): Promise<void> {
    const tabText = tabName === 'people' ? '👥 People Management' : '🎭 Roles Management';
    await expect(this.page.locator(`button:has-text("${tabText}").active`)).toBeVisible();
  }
}