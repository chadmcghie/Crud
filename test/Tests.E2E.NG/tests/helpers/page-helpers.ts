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
    // Wait for the app to be interactive (buttons clickable)
    await this.page.waitForFunction(() => {
      const button = document.querySelector('button');
      return button && !button.disabled;
    });
  }

  async switchToPeopleTab(): Promise<void> {
    await this.page.click('button:has-text("👥 People Management")');
    await this.page.waitForSelector('app-people-list', { timeout: 10000 });
    // Wait for the people content to be fully rendered
    await this.page.waitForSelector('h3:has-text("People Directory")', { timeout: 5000 });
  }

  async switchToRolesTab(): Promise<void> {
    await this.page.click('button:has-text("🎭 Roles Management")');
    await this.page.waitForSelector('app-roles-list', { timeout: 10000 });
    // Wait for the roles content to be fully rendered
    await this.page.waitForSelector('h3:has-text("Roles Management")', { timeout: 5000 });
  }

  // Role management helpers
  async clickAddRole(): Promise<void> {
    await this.retryOperation(async () => {
      await this.page.click('button:has-text("Add New Role")');
      await this.page.waitForSelector('app-roles form', { timeout: 10000 });
      // Wait for form fields to be ready
      await this.page.waitForSelector('input#name', { timeout: 5000 });
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
      
      // Add small delay to ensure form is ready
      await this.page.waitForTimeout(100);
      
      await this.page.click('button[type="submit"]:has-text("Create Role")');

      // Wait for API response
      await this.page.waitForResponse(response => 
        response.url().includes('/api/roles') && response.status() === 201,
        { timeout: 10000 }
      ).catch(() => {
        // If no API call, wait for form to hide
        return this.page.waitForSelector('app-roles form', { state: 'hidden', timeout: 10000 });
      });
      
      // Give UI time to update
      await this.page.waitForTimeout(200);
    }, 3, 1000, 'submitRoleForm');
  }

  async editRole(roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`).first();
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
    }
  }

  async deleteRole(roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`).first();
    
    // Wait for the row to be visible first
    await roleRow.waitFor({ state: 'visible', timeout: 10000 });
    
    // Handle the confirmation dialog - use once() to avoid multiple handlers
    this.page.once('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await roleRow.locator('button:has-text("Delete")').click();
    
    // Wait for the role to actually be removed from the DOM
    await this.page.waitForFunction(
      (name) => {
        const rows = document.querySelectorAll('tr');
        const element = Array.from(rows).find(row => row.textContent?.includes(name));
        return !element;
      },
      roleName,
      { timeout: 10000 }
    );
    
    // Additional wait for UI to stabilize
  }

  async getRoleRowCount(): Promise<number> {
    const rows = await this.page.locator('.roles-table tbody tr').count();
    return rows;
  }

  async verifyRoleExists(roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    await expect(this.page.locator(`tr:has-text("${roleName}")`).first()).toBeVisible({ timeout: 10000 });
  }

  async verifyRoleNotExists(roleName: string): Promise<void> {
    // Check that no rows with this text exist
    await expect(this.page.locator(`tr:has-text("${roleName}")`)).toHaveCount(0);
  }

  // Person management helpers
  async clickAddPerson(): Promise<void> {
    await this.page.click('button:has-text("Add New Person")');
    await this.page.waitForSelector('app-people form', { timeout: 10000 });
    // Wait for form fields to be ready and interactable
    await this.page.waitForSelector('input#fullName', { timeout: 5000 });
    await this.page.waitForFunction(() => {
      const input = document.querySelector('input#fullName') as HTMLInputElement;
      return input && !input.disabled;
    }, { timeout: 5000 });
  }

  async fillPersonForm(fullName: string, phone?: string, roleNames?: string[]): Promise<void> {
    await this.page.fill('input#fullName', fullName);
    if (phone) {
      // Clear the phone field first, then fill with new value
      await this.page.fill('input#phone', '');
      await this.page.fill('input#phone', phone);
    }
    
    if (roleNames && roleNames.length > 0) {
      // Wait for roles checkboxes to be loaded in the form
      await this.page.waitForSelector('input[type="checkbox"]', { timeout: 5000 });
      await this.page.waitForFunction(() => {
        const checkboxes = document.querySelectorAll('input[type="checkbox"]');
        return checkboxes.length > 0;
      }, { timeout: 5000 });
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
    
    // Add small delay to ensure form is ready
    await this.page.waitForTimeout(100);
    
    await this.page.click('button[type="submit"]:has-text("Create Person")');
    
    // Wait for API response
    await this.page.waitForResponse(response => 
      response.url().includes('/api/people') && response.status() === 201,
      { timeout: 10000 }
    ).catch(() => {
      // If no API call, wait for form to hide
      return this.page.waitForSelector('app-people form', { state: 'hidden', timeout: 10000 });
    });
    
    // Give UI time to update
    await this.page.waitForTimeout(200);
  }

  async editPerson(personName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const personRow = this.page.locator(`tr:has-text("${personName}")`).first();
    
    // Wait for the row to be visible first
    await personRow.waitFor({ state: 'visible', timeout: 10000 });
    
    // Click the edit button
    await personRow.locator('button:has-text("Edit")').click();
    
    // Wait for the form to appear and be ready
    await this.page.waitForSelector('app-people form', { timeout: 10000 });
    await this.page.waitForSelector('input#fullName', { timeout: 5000 });
    
    // Additional wait for form to be fully loaded
  }

  async updatePersonForm(): Promise<void> {
    // Add small delay to ensure form is ready
    await this.page.waitForTimeout(100);
    
    await this.page.click('button[type="submit"]:has-text("Update Person")');
    
    // Wait for API response
    await this.page.waitForResponse(response => 
      response.url().includes('/api/people') && 
      (response.status() === 200 || response.status() === 204),
      { timeout: 10000 }
    ).catch(() => {
      // If no API call, wait for form to hide
      return this.page.waitForSelector('app-people form', { state: 'hidden', timeout: 10000 });
    });
    
    // Give UI time to update
    await this.page.waitForTimeout(200);
  }

  async deletePerson(personName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const personRow = this.page.locator(`tr:has-text("${personName}")`).first();
    
    // Wait for the row to be visible first
    await personRow.waitFor({ state: 'visible', timeout: 10000 });
    
    // Handle the confirmation dialog - use once() to avoid multiple handlers
    this.page.once('dialog', async dialog => {
      expect(dialog.type()).toBe('confirm');
      await dialog.accept();
    });
    
    await personRow.locator('button:has-text("Delete")').click();
    
    // Wait for the person to actually be removed from the DOM
    await this.page.waitForFunction(
      (name) => {
        const rows = document.querySelectorAll('tr');
        const element = Array.from(rows).find(row => row.textContent?.includes(name));
        return !element;
      },
      personName,
      { timeout: 10000 }
    );
    
    // Additional wait for UI to stabilize
  }

  async getPersonRowCount(): Promise<number> {
    const rows = await this.page.locator('.people-table tbody tr').count();
    return rows;
  }

  async verifyPersonExists(personName: string): Promise<void> {
    // Use retry logic with proper timeout
    await this.page.waitForSelector(`tr:has-text("${personName}")`, { timeout: 10000 });
    // Use first() to handle multiple matches in strict mode
    await expect(this.page.locator(`tr:has-text("${personName}")`).first()).toBeVisible();
  }

  async verifyPersonNotExists(personName: string): Promise<void> {
    // Wait for the element to be removed or not exist
    try {
      await this.page.waitForFunction(
        (name) => {
          const rows = document.querySelectorAll('tr');
          const element = Array.from(rows).find(row => row.textContent?.includes(name));
          return !element;
        },
        personName,
        { timeout: 10000 }
      );
    } catch (error) {
      // Element might not exist at all, which is fine
    }
    // Check that no rows with this text exist
    await expect(this.page.locator(`tr:has-text("${personName}")`)).toHaveCount(0);
  }

  async verifyPersonHasRole(personName: string, roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const personRow = this.page.locator(`tr:has-text("${personName}")`).first();
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
      }, 3, 2000, 'refreshPage');
  }

  async clickRefreshButton(): Promise<void> {
    await this.page.click('button:has-text("Refresh")');
    // Wait for refresh to complete
    await this.page.waitForResponse(response => 
      response.url().includes('/api/') && response.ok(),
      { timeout: 5000 }
    ).catch(() => {
      // If no API call, wait for UI to update
      return this.page.waitForTimeout(500);
    });
  }

  async verifyEmptyState(entityType: 'roles' | 'people'): Promise<void> {
    const emptyStateText = entityType === 'roles' 
      ? 'No roles found. Add the first role'
      : 'No people found. Add the first person';
    
    // Wait for the page to load and check for empty state or table
    // Wait for table data to load
    await this.page.waitForFunction(() => {
      const rows = document.querySelectorAll('tbody tr');
      return rows.length > 0 || document.querySelector('.empty-state');
    });
    
    // Try to find empty state first, if not found, check if table is empty
    const emptyState = this.page.locator('.empty-state');
    const tableRows = this.page.locator(entityType === 'roles' ? '.roles-table tbody tr' : '.people-table tbody tr');
    
    // Wait for either empty state or table to be present
    try {
      await this.page.waitForSelector('.empty-state, .people-table tbody, .roles-table tbody', { timeout: 5000 });
    } catch (error) {
      console.warn('Neither empty state nor table found, waiting longer...');
    }
    
    const emptyStateVisible = await emptyState.isVisible().catch(() => false);
    if (emptyStateVisible) {
      await expect(emptyState).toContainText(emptyStateText);
    } else {
      // If no empty state element, verify table is empty
      // Wait a bit for any rows to appear if they're going to
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