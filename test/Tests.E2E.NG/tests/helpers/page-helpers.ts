// Page object helpers for Angular UI interactions
import { Page, Locator, expect } from '@playwright/test';

export class PageHelpers {
  constructor(private page: Page) {}

  // Add retry logic similar to API helpers
  private async retryOperation<T>(
    operation: () => Promise<T>,
    maxRetries: number = 3,
    delayMs: number = 500,
    operationName: string = 'operation'
  ): Promise<T> {
    let lastError: Error;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error as Error;
        console.warn(`${operationName} failed (attempt ${attempt}/${maxRetries}):`, error);
        
        if (attempt < maxRetries) {
          // Random delay to prevent thundering herd
          const randomDelay = delayMs + Math.random() * 200;
          await new Promise(resolve => setTimeout(resolve, randomDelay));
        }
      }
    }
    
    throw lastError!;
  }

  // Navigation helpers
  async navigateToApp(): Promise<void> {
    await this.page.goto('/');
    // Wait for the main app component to be fully loaded - this is more reliable than networkidle
    await this.page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 30000 });
    // Wait for Angular to initialize and render the main content
    await this.page.waitForSelector('button:has-text("👥 People Management")', { timeout: 15000 });
    // Small buffer to ensure everything is ready
    await this.page.waitForTimeout(1000);
  }

  async switchToPeopleTab(): Promise<void> {
    await this.page.click('button:has-text("👥 People Management")');
    await this.page.waitForSelector('app-people-list', { timeout: 10000 });
    // Wait for the people content to be fully rendered
    await this.page.waitForSelector('h3:has-text("People Directory")', { timeout: 5000 });
    await this.page.waitForTimeout(300);
  }

  async switchToRolesTab(): Promise<void> {
    await this.page.click('button:has-text("🎭 Roles Management")');
    await this.page.waitForSelector('app-roles-list', { timeout: 10000 });
    // Wait for the roles content to be fully rendered
    await this.page.waitForSelector('h3:has-text("Roles Management")', { timeout: 5000 });
    await this.page.waitForTimeout(300);
  }

  // Role management helpers
  async clickAddRole(): Promise<void> {
    await this.retryOperation(async () => {
      await this.page.click('button:has-text("Add New Role")');
      await this.page.waitForSelector('app-roles form', { timeout: 10000 });
      // Wait for form fields to be ready
      await this.page.waitForSelector('input#name', { timeout: 5000 });
      await this.page.waitForTimeout(200);
    }, 3, 500, 'clickAddRole');
  }

  async fillRoleForm(name: string, description?: string): Promise<void> {
    await this.page.fill('input#name', name);
    if (description) {
      await this.page.fill('textarea#description', description);
    }
  }

  async submitRoleForm(): Promise<void> {
    await this.retryOperation(async () => {
      // Wait for submit button to be enabled
      await this.page.waitForSelector('button[type="submit"]:has-text("Create Role"):not([disabled])', { timeout: 5000 });
      await this.page.click('button[type="submit"]:has-text("Create Role")');

      // Wait for form to be hidden or for a success indicator
      try {
        await this.page.waitForSelector('app-roles form', { state: 'hidden', timeout: 10000 });
      } catch (error) {
        // If form doesn't hide, check if submission was successful by looking for the new role
        await this.page.waitForTimeout(2000);
        // Verify submission worked by checking if we can see roles table
        await this.page.waitForSelector('.roles-table', { timeout: 5000 });
      }
    }, 3, 1000, 'submitRoleForm');
  }

  async editRole(roleName: string): Promise<void> {
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`);
    await roleRow.locator('button:has-text("Edit")').click();
    await this.page.waitForSelector('app-roles form');
  }

  async updateRoleForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Update Role")');
    // Wait for form to be hidden or for a success indicator
    try {
      await this.page.waitForSelector('app-roles form', { state: 'hidden', timeout: 10000 });
    } catch (error) {
      // If form doesn't hide, check if update was successful
      await this.page.waitForTimeout(2000);
    }
  }

  async deleteRole(roleName: string): Promise<void> {
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`);
    
    // Handle the confirmation dialog - use once() to avoid multiple handlers
    this.page.once('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await roleRow.locator('button:has-text("Delete")').click();
    // Wait for deletion to complete and UI to refresh
    await this.page.waitForTimeout(1000);
    // Verify the role is actually removed from the DOM
    await this.page.waitForFunction(
      (name) => {
        const rows = document.querySelectorAll('tr');
        const element = Array.from(rows).find(row => row.textContent?.includes(name)) as HTMLElement;
        return !element || element.style.display === 'none';
      },
      roleName,
      { timeout: 5000 }
    );
  }

  async getRoleRowCount(): Promise<number> {
    const rows = await this.page.locator('.roles-table tbody tr').count();
    return rows;
  }

  async verifyRoleExists(roleName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${roleName}")`)).toBeVisible({ timeout: 10000 });
  }

  async verifyRoleNotExists(roleName: string): Promise<void> {
    await expect(this.page.locator(`tr:has-text("${roleName}")`)).not.toBeVisible();
  }

  // Person management helpers
  async clickAddPerson(): Promise<void> {
    await this.page.click('button:has-text("Add New Person")');
    await this.page.waitForSelector('app-people form', { timeout: 10000 });
    // Wait for form fields to be ready
    await this.page.waitForSelector('input#fullName', { timeout: 5000 });
    await this.page.waitForTimeout(200);
  }

  async fillPersonForm(fullName: string, phone?: string, roleNames?: string[]): Promise<void> {
    await this.page.fill('input#fullName', fullName);
    if (phone) {
      await this.page.fill('input#phone', phone);
    }
    
    if (roleNames && roleNames.length > 0) {
      // Wait for roles to load in the form
      await this.page.waitForTimeout(2000); // Give time for roles to load
      
      // First, uncheck all roles
      const checkboxes = await this.page.locator('input[type="checkbox"]').all();
      for (const checkbox of checkboxes) {
        if (await checkbox.isChecked()) {
          await checkbox.uncheck();
        }
      }
      
      // Then check the specified roles
      for (const roleName of roleNames) {
        // Use getByRole to find checkboxes by their accessible name
        try {
          const roleCheckbox = this.page.getByRole('checkbox', { name: new RegExp(roleName, 'i') });
          await roleCheckbox.check();
        } catch (error) {
          console.warn(`Could not find or check checkbox for role: ${roleName}`, error);
          // Fallback: try to find by text content
          try {
            const fallbackCheckbox = this.page.locator(`input[type="checkbox"]`).locator(`xpath=//input[@type='checkbox'][..//*[contains(text(), '${roleName}')]]`).first();
            await fallbackCheckbox.check();
          } catch (fallbackError) {
            console.warn(`Fallback also failed for role: ${roleName}`, fallbackError);
          }
        }
      }
    }
  }

  async submitPersonForm(): Promise<void> {
    // Wait for submit button to be enabled
    await this.page.waitForSelector('button[type="submit"]:has-text("Create Person"):not([disabled])', { timeout: 5000 });
    await this.page.click('button[type="submit"]:has-text("Create Person")');
    

    // Wait for either form to hide OR success state (new person appears in table)
    try {
      await Promise.race([
        this.page.waitForSelector('app-people form', { state: 'hidden', timeout: 10000 }),
        this.page.waitForSelector('.person-row', { timeout: 10000 }) // Wait for new person to appear
      ]);
    } catch (error) {
      // Fallback: wait for network to be idle
      await this.page.waitForLoadState('networkidle', { timeout: 5000 });
    }

  }

  async editPerson(personName: string): Promise<void> {
    const personRow = this.page.locator(`tr:has-text("${personName}")`);
    await personRow.locator('button:has-text("Edit")').click();
    await this.page.waitForSelector('app-people form');
  }

  async updatePersonForm(): Promise<void> {
    await this.page.click('button[type="submit"]:has-text("Update Person")');
    
    // Wait for either form to hide OR success state
    try {
      await Promise.race([
        this.page.waitForSelector('app-people form', { state: 'hidden', timeout: 10000 }),
        this.page.waitForLoadState('networkidle', { timeout: 5000 })
      ]);
    } catch (error) {
      // Fallback: small wait
      await this.page.waitForTimeout(1000);
    }
  }

  async deletePerson(personName: string): Promise<void> {
    const personRow = this.page.locator(`tr:has-text("${personName}")`);
    
    // Handle the confirmation dialog - use once() to avoid multiple handlers
    this.page.once('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await personRow.locator('button:has-text("Delete")').click();

    
    // Wait for the person to actually be removed from the table
    try {
      await this.page.waitForFunction(
        (name) => !document.querySelector(`tr:has-text("${name}")`)?.isConnected,
        personName,
        { timeout: 10000 }
      );
    } catch (error) {
      // Fallback: wait for network to be idle
      await this.page.waitForLoadState('networkidle', { timeout: 5000 });
    }

  }

  async getPersonRowCount(): Promise<number> {
    const rows = await this.page.locator('.people-table tbody tr').count();
    return rows;
  }

  async verifyPersonExists(personName: string): Promise<void> {
    // Use retry logic with proper timeout
    await this.page.waitForSelector(`tr:has-text("${personName}")`, { timeout: 10000 });
    await expect(this.page.locator(`tr:has-text("${personName}")`)).toBeVisible();

  }

  async verifyPersonNotExists(personName: string): Promise<void> {
    // Wait for the element to be removed or not exist
    try {
      await this.page.waitForFunction(
        (name) => !document.querySelector(`tr:has-text("${name}")`)?.isConnected,
        personName,
        { timeout: 10000 }
      );
    } catch (error) {
      // Element might not exist at all, which is fine
    }
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
    await this.retryOperation(async () => {
      await this.page.reload();
      // Instead of waiting for networkidle, wait for specific content to be ready
      await this.page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 30000 });
      await this.page.waitForSelector('button:has-text("👥 People Management")', { timeout: 15000 });
      // Small buffer for Angular to stabilize
      await this.page.waitForTimeout(1000);
    }, 3, 2000, 'refreshPage');
  }

  async clickRefreshButton(): Promise<void> {
    await this.page.click('button:has-text("Refresh")');
    await this.page.waitForTimeout(500);
  }

  async verifyEmptyState(entityType: 'roles' | 'people'): Promise<void> {
    const emptyStateText = entityType === 'roles' 

      ? 'No roles found. Add the first role'
      : 'No people found. Add the first person';
    
    // Wait for the page to load and check for empty state or table
    await this.page.waitForTimeout(1000);
    
    // Try to find empty state first, if not found, check if table is empty
    const emptyState = this.page.locator('.empty-state');
    const tableRows = this.page.locator(entityType === 'roles' ? '.roles-table tbody tr' : '.people-table tbody tr');
    
    const emptyStateVisible = await emptyState.isVisible().catch(() => false);
    if (emptyStateVisible) {
      await expect(emptyState).toContainText(emptyStateText);
    } else {
      // If no empty state element, verify table is empty
      const rowCount = await tableRows.count();
      expect(rowCount).toBe(0);
    }

  }

  async verifyPageTitle(): Promise<void> {
    await expect(this.page.locator('h1')).toContainText('People & Roles Management System');
  }

  async verifyTabActive(tabName: 'people' | 'roles'): Promise<void> {
    const tabText = tabName === 'people' ? '👥 People Management' : '🎭 Roles Management';
    await expect(this.page.locator(`button:has-text("${tabText}").active`)).toBeVisible();
  }
}