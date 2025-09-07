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
    await this.page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 30000 });
    // Wait for Angular to initialize and render the main content
    await this.page.waitForSelector('a[routerLink="/people-list"]', { timeout: 15000 });
    // Wait for the app to be interactive (links clickable)
    await this.page.waitForFunction(() => {
      const link = document.querySelector('a[routerLink="/people-list"]');
      return link !== null;
    });
  }

  async switchToPeopleTab(): Promise<void> {
    await this.page.click('a[routerLink="/people-list"]');
    await this.page.waitForSelector('app-people-list', { timeout: 10000 });
    // Wait for the people content to be fully rendered
    await this.page.waitForSelector('h3:has-text("People Directory")', { timeout: 5000 });
  }

  async switchToRolesTab(): Promise<void> {
    await this.page.click('a[routerLink="/roles-list"]');
    await this.page.waitForSelector('app-roles-list', { timeout: 10000 });
    // Wait for the roles content to be fully rendered
    await this.page.waitForSelector('h3:has-text("Roles Management")', { timeout: 5000 });
  }

  // Role management helpers
  async clickAddRole(): Promise<void> {
    await this.retryOperation(async () => {
      // Click the Add New Role button which should navigate to the roles form
      await this.page.click('button:has-text("Add New Role")');
      
      // Wait for navigation to the roles form route
      await this.page.waitForURL('**/roles', { timeout: 10000 });
      
      // Wait for the roles form component to load and be ready
      await this.page.waitForSelector('app-roles', { timeout: 10000 });
      
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
      // Wait for submit button to be enabled (handle both Create and Update)
      await this.page.waitForSelector('button[type="submit"]:not([disabled])', { timeout: 5000 });
      
      // Add small delay to ensure form is ready
      await this.page.waitForTimeout(100);
      
      // Click the submit button
      await this.page.click('button[type="submit"]');

      // Wait for navigation back to the roles-list after successful submission
      await this.page.waitForURL('**/roles-list', { timeout: 10000 });
      
      // Wait for the list component to load
      await this.page.waitForSelector('app-roles-list', { timeout: 5000 });
      
      // Give UI time to update with new data
      await this.page.waitForTimeout(500);
    }, 3, 1000, 'submitRoleForm');
  }

  async editRole(roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`).first();
    
    // Click the edit button which should navigate to the roles form with edit query param
    await roleRow.locator('button:has-text("Edit")').click();
    
    // Wait for navigation to the roles form route with edit parameter
    await this.page.waitForURL('**/roles?edit=*', { timeout: 10000 });
    
    // Wait for the roles form component to load
    await this.page.waitForSelector('app-roles', { timeout: 10000 });
    
    // Wait for form to be populated with existing data
    await this.page.waitForFunction(() => {
      const input = document.querySelector('input#name') as HTMLInputElement;
      return input && input.value && input.value.trim().length > 0;
    }, { timeout: 5000 });
  }

  async updateRoleForm(): Promise<void> {
    // Use the same submission logic for both create and update
    await this.submitRoleForm();
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
    // Click the Add New Person button which should navigate to the people form
    await this.page.click('button:has-text("Add New Person")');
    
    // Wait for navigation to the people form route
    await this.page.waitForURL('**/people', { timeout: 10000 });
    
    // Wait for the people form component to load and be ready
    await this.page.waitForSelector('app-people', { timeout: 10000 });
    
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
    // Wait for submit button to be enabled (handle both Create and Update)
    await this.page.waitForSelector('button[type="submit"]:not([disabled])', { timeout: 5000 });
    
    // Add small delay to ensure form is ready
    await this.page.waitForTimeout(100);
    
    // Click the submit button (works for both Create and Update)
    await this.page.click('button[type="submit"]');
    
    // Wait for navigation back to the people-list after successful submission
    await this.page.waitForURL('**/people-list', { timeout: 10000 });
    
    // Wait for the list component to load
    await this.page.waitForSelector('app-people-list', { timeout: 5000 });
    
    // Give UI time to update with new data
    await this.page.waitForTimeout(500);
  }

  async editPerson(personName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const personRow = this.page.locator(`tr:has-text("${personName}")`).first();
    
    // Wait for the row to be visible first
    await personRow.waitFor({ state: 'visible', timeout: 10000 });
    
    // Click the edit button which should navigate to the people form with edit query param
    await personRow.locator('button:has-text("Edit")').click();
    
    // Wait for navigation to the people form route with edit parameter
    await this.page.waitForURL('**/people?edit=*', { timeout: 10000 });
    
    // Wait for the people form component to load
    await this.page.waitForSelector('app-people', { timeout: 10000 });
    await this.page.waitForSelector('input#fullName', { timeout: 5000 });
    
    // Wait for form to be populated with existing data
    await this.page.waitForFunction(() => {
      const input = document.querySelector('input#fullName') as HTMLInputElement;
      return input && input.value && input.value.trim().length > 0;
    }, { timeout: 5000 });
  }

  async updatePersonForm(): Promise<void> {
    // Use the same submission logic for both create and update
    await this.submitPersonForm();
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
      await this.page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 30000 });
      await this.page.waitForSelector('a[routerLink="/people-list"]', { timeout: 15000 });
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
    await expect(this.page.locator('h1')).toContainText('CRUD Template Application');
  }

  async verifyTabActive(tabName: 'people' | 'roles'): Promise<void> {
    // Since we're using router links instead of tabs, verify the correct page is loaded
    if (tabName === 'people') {
      await expect(this.page.locator('app-people-list')).toBeVisible();
    } else {
      await expect(this.page.locator('app-roles-list')).toBeVisible();
    }
  }
}
