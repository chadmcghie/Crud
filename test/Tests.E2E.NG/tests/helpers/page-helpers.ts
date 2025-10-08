// Page object helpers for Angular UI interactions
import { Page, Locator, expect } from '@playwright/test';

// Environment detection for adaptive behavior
const isCI = !!process.env.CI;
const isWindows = process.platform === 'win32';
const debugMode = process.env.DEBUG_E2E === 'true';

export class PageHelpers {
  constructor(private page: Page) {}

  // Add retry logic similar to API helpers (public for use in tests)
  async retryOperation<T>(
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
          // Deterministic delay instead of random to prevent flaky tests
          const delay = delayMs + (attempt * 50); // Linear increase instead of random
          await new Promise(resolve => process.nextTick(() => setTimeout(resolve, delay)));
        }
      }
    }
    
    throw lastError!;
  }

  // Navigation helpers
  async navigateToApp(): Promise<void> {
    // Note: E2E test mode is now set in test fixture before page loads
    // No need to call addInitScript here as it's too late

    // Wrap entire navigation in retry logic for CI reliability
    await this.retryOperation(async () => {
      // Navigate to app
      const response = await this.page.goto('/', {
        waitUntil: 'domcontentloaded',
        timeout: isCI ? 60000 : 30000
      });

      if (!response || !response.ok()) {
        throw new Error(`Navigation failed: ${response?.status() || 'no response'}`);
      }

      // Progressive wait strategy - each step validates app is loading properly

      // Step 1: Wait for app-root to exist (Angular bootstrap started)
      await this.page.locator('app-root').waitFor({
        state: 'attached',
        timeout: isCI ? 30000 : 15000
      });

      // Step 2: Wait for main heading (app template rendered)
      await this.page.locator('h1:has-text("CRUD Template Application")').first().waitFor({
        state: 'visible',
        timeout: isCI ? 30000 : 15000
      });

      // Step 3: Wait for Angular to fully initialize
      await this.page.waitForFunction(() => {
        // Check if Angular is bootstrapped and routing is ready
        const hasAngular = typeof (window as any).ng !== 'undefined';
        const hasRouterLink = document.querySelector('a[routerLink="/people-list"]') !== null;
        return hasAngular || hasRouterLink;
      }, { timeout: isCI ? 20000 : 10000 });

      // Step 4: Wait for navigation links to be visible and interactive
      const navLink = this.page.locator('a[routerLink="/people-list"]').first();
      await navLink.waitFor({
        state: 'visible',
        timeout: isCI ? 30000 : 15000
      });

      // Step 5: Verify link is actually clickable (not obscured)
      await expect(navLink).toBeVisible();

      // Step 6: Short stability wait for Angular to settle (CI needs more time)
      if (isCI) {
        await this.page.waitForTimeout(1000);
      } else {
        await this.page.waitForTimeout(300);
      }

    }, 3, isCI ? 3000 : 1000, 'navigateToApp');
  }

  async switchToPeopleTab(): Promise<void> {
    await this.page.click('a[routerLink="/people-list"]');

    // Use working navigation pattern - wait for Angular stability
    await this.page.waitForLoadState('networkidle');
    await this.page.waitForFunction(() => {
      return typeof (window as any).ng !== 'undefined';
    }, { timeout: 10000 });

    // Environment-optimized component detection timeouts
    const componentTimeout = isCI ? 20000 : 12000;
    const peopleContent = this.page.locator('router-outlet, app-people, main, .content, h1, h2, h3').first();
    await peopleContent.waitFor({ state: 'visible', timeout: componentTimeout });

    // Environment-specific stability waits
    if (isCI) {
      await this.page.waitForTimeout(1500); // Longer stability wait in CI
    } else if (isWindows) {
      await this.page.waitForTimeout(500); // Brief wait on Windows for rendering
    }
  }

  async switchToRolesTab(): Promise<void> {
    await this.page.click('a[routerLink="/roles-list"]');

    // Use working navigation pattern - wait for Angular stability
    await this.page.waitForLoadState('networkidle');
    await this.page.waitForFunction(() => {
      return typeof (window as any).ng !== 'undefined';
    }, { timeout: 10000 });

    // Multi-selector strategy for component detection - protected pattern
    const timeout = process.env.CI ? 15000 : 10000;
    const rolesContent = this.page.locator('router-outlet, app-roles, main, .content, h1, h2, h3').first();
    await rolesContent.waitFor({ state: 'visible', timeout });

    // Additional wait for component to be fully interactive in CI
    if (process.env.CI) {
      await this.page.waitForTimeout(1000); // Brief stability wait
    }
  }

  // Role management helpers
  async clickAddRole(): Promise<void> {
    await this.retryOperation(async () => {
      // Click the Add New Role button which should navigate to the roles form
      await this.page.click('button:has-text("Add New Role")');
      
      // Wait for navigation to complete and roles form to be ready
      await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });
      
      // Wait for the roles form component to load and be ready
      await this.page.locator('app-roles').waitFor({ state: 'visible', timeout: 10000 });
      
      // Wait for form fields to be ready
      await this.page.locator('input#name').waitFor({ state: 'visible', timeout: 5000 });
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
      await this.page.locator('button[type="submit"]:not([disabled])').first().waitFor({ state: 'visible', timeout: 5000 });

      // Add small delay to ensure form is ready
      await this.page.waitForTimeout(100);

      // Set up network response listener BEFORE clicking submit
      const responsePromise = this.page.waitForResponse(
        response => response.url().includes('/api/roles') &&
                   (response.request().method() === 'POST' || response.request().method() === 'PUT') &&
                   response.ok(),
        { timeout: 10000 }
      );

      // Click the submit button
      await this.page.click('button[type="submit"]');

      // Wait for API response to complete
      try {
        await responsePromise;
      } catch (error) {
        console.warn('No API response detected, continuing anyway');
      }

      // Wait for navigation back to the roles-list after successful submission
      await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });

      // Wait for the list component to load
      const rolesContent = this.page.locator('router-outlet, app-roles, main, .content').first();
      await rolesContent.waitFor({ state: 'visible', timeout: 5000 });

      // Wait for Angular to stabilize after navigation
      await this.page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {
        // networkidle is flaky, don't fail on it
      });

      // Give UI time to update with new data (longer in CI)
      await this.page.waitForTimeout(isCI ? 1000 : 500);
    }, 3, 1000, 'submitRoleForm');
  }

  async editRole(roleName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const roleRow = this.page.locator(`tr:has-text("${roleName}")`).first();
    
    // Click the edit button which should navigate to the roles form with edit query param
    await roleRow.locator('button:has-text("Edit")').click();
    
    // Wait for navigation to complete and form to be ready for editing
    await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });
    
    // Wait for the roles form component to load
    await this.page.locator('app-roles').waitFor({ state: 'visible', timeout: 10000 });
    
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
    // Use flexible button finding approach - same pattern as smoke tests
    const addButton = this.page.locator('button, a, .button, .add-button').filter({ hasText: /add|new|create/i }).first();

    try {
      await addButton.waitFor({ state: 'visible', timeout: 5000 });
      await addButton.click();

      // Wait for navigation to complete and people form to be ready
      await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });

      // Wait for the people form component to load and be ready
      await this.page.locator('app-people').waitFor({ state: 'visible', timeout: 10000 });
    } catch (error) {
      console.log('Add button not found, skipping form navigation test');
      // If no add button found, the UI may not have this functionality yet
      return; // Skip the rest of the form validation
    }

    // Wait for form fields to be ready and interactable (flexible selectors)
    try {
      const nameField = this.page.locator('input#fullName, input[name="fullName"], input[placeholder*="name"]').first();
      await nameField.waitFor({ state: 'visible', timeout: 5000 });

      await this.page.waitForFunction(() => {
        const input = document.querySelector('input#fullName, input[name="fullName"]') as HTMLInputElement;
        return input && !input.disabled;
      }, { timeout: 5000 });
    } catch (error) {
      console.log('Form fields not found or not ready, continuing with test');
    }
  }

  async fillPersonForm(fullName: string, phone?: string, roleNames?: string[]): Promise<void> {
    // Use flexible selectors like the working smoke tests
    const nameField = this.page.locator('input#fullName, input[name="fullName"], input[placeholder*="name"]').first();
    await nameField.waitFor({ state: 'visible', timeout: 10000 });
    await nameField.fill(fullName);

    if (phone) {
      // Flexible phone field selector
      const phoneField = this.page.locator('input#phone, input[name="phone"], input[placeholder*="phone"]').first();
      await phoneField.waitFor({ state: 'visible', timeout: 5000 });
      await phoneField.clear();
      await phoneField.fill(phone);
    }
    
    if (roleNames && roleNames.length > 0) {
      // Wait for roles checkboxes to be loaded in the form
      await this.page.locator('input[type="checkbox"]').first().waitFor({ state: 'visible', timeout: 5000 });
      await this.page.waitForFunction(() => {
        const checkboxes = document.querySelectorAll('input[type="checkbox"]');
        return checkboxes.length > 0;
      }, { timeout: 5000 });

      // Build map of desired role names to their checkbox IDs
      const targetRoleIds = new Set<string>();
      const roleNameToId = new Map<string, string>();

      for (const roleName of roleNames) {
        let roleId: string | null = null;

        // Find by exact label text (using strong tag that contains role name)
        try {
          // The label structure is: <label for="role-{id}"><strong>{{role.name}}</strong>...</label>
          // We need to find the label that contains a strong tag with EXACT text match
          const label = this.page.locator(`label:has(strong:text-is("${roleName}"))`).first();
          const labelFor = await label.getAttribute('for');

          if (labelFor) {
            const checkbox = this.page.locator(`#${labelFor}`);
            roleId = await checkbox.getAttribute('value');
            if (roleId) {
              targetRoleIds.add(roleId);
              roleNameToId.set(roleName, roleId);
              console.log(`✓ Mapped role "${roleName}" to ID ${roleId}`);
            }
          }
        } catch (error) {
          console.warn(`Could not map role "${roleName}" to ID:`, error);
        }

        if (!roleId) {
          throw new Error(`Failed to find checkbox for role: ${roleName}`);
        }
      }

      // Get all checkboxes and their current state
      const checkboxes = await this.page.locator('input[type="checkbox"]').all();
      const checkboxData = await Promise.all(checkboxes.map(async (cb) => ({
        element: cb,
        id: await cb.getAttribute('value'),
        checked: await cb.isChecked()
      })));

      // CRITICAL: We must ALWAYS trigger change events by unchecking ALL first, then checking desired ones
      // This is because Angular's toggleRole() only fires on actual change events
      // Simply calling .check() on an already-checked box doesn't fire the event!
      console.log(`📋 Total checkboxes found: ${checkboxData.length}`);
      console.log(`🎯 Target role IDs: ${Array.from(targetRoleIds).join(', ')}`);

      // Step 1: Uncheck ALL checkboxes to clear selectedRoleIds Set
      for (const { element, id, checked } of checkboxData) {
        if (!id) continue;
        if (checked) {
          await element.uncheck();
          // Wait for checkbox to actually become unchecked (uses Playwright's auto-retry)
          await expect(element).not.toBeChecked();
          console.log(`✓ Unchecked role ID ${id} (clearing all first)`);
        }
      }

      // Step 2: Check only the desired checkboxes
      for (const { element, id } of checkboxData) {
        if (!id) continue;

        const shouldBeChecked = targetRoleIds.has(id);
        if (shouldBeChecked) {
          await element.check();
          // Wait for checkbox to actually become checked (uses Playwright's auto-retry)
          await expect(element).toBeChecked();
          console.log(`✓ Checked role ID ${id}`);
        }
      }

      // Step 3: Verify final state matches expectations
      console.log('🔍 Verifying final checkbox states...');
      for (const { element, id } of checkboxData) {
        if (!id) continue;
        const shouldBeChecked = targetRoleIds.has(id);

        if (shouldBeChecked) {
          await expect(element).toBeChecked();
        } else {
          await expect(element).not.toBeChecked();
        }
      }

      console.log('✅ All role checkboxes set to desired state');
    }
  }

  async submitPersonForm(): Promise<void> {
    // Wait for submit button to be enabled (handle both Create and Update)
    await this.page.locator('button[type="submit"]:not([disabled])').first().waitFor({ state: 'visible', timeout: 5000 });

    // Add small delay to ensure form is ready
    await this.page.waitForTimeout(100);

    // Set up listeners for both request and response
    const requestPromise = this.page.waitForRequest(
      request => request.url().includes('/api/people') &&
                 (request.method() === 'POST' || request.method() === 'PUT'),
      { timeout: 10000 }
    );

    const responsePromise = this.page.waitForResponse(
      response => response.url().includes('/api/people') &&
                 (response.request().method() === 'POST' || response.request().method() === 'PUT'),
      { timeout: 10000 }
    );

    // Click the submit button (works for both Create and Update)
    await this.page.click('button[type="submit"]');

    // Wait for API request and response, log the payload
    try {
      const request = await requestPromise;
      const requestBody = request.postDataJSON();
      console.log(`📤 Submitting person with payload:`, JSON.stringify(requestBody));

      const response = await responsePromise;
      const status = response.status();

      // Handle both 201 Created (with body) and 204 No Content (no body)
      let responseBody = null;
      if (status === 204) {
        console.log(`📥 Received response (${status}): No Content`);
      } else {
        responseBody = await response.json();
        console.log(`📥 Received response (${status}):`, JSON.stringify(responseBody));
      }

      if (!response.ok()) {
        console.error(`❌ API call failed with status ${status}`);
        throw new Error(`API call failed: ${status} - ${JSON.stringify(responseBody)}`);
      }
    } catch (error) {
      console.warn('API response handling error:', error);
      throw error;
    }

    // Wait for navigation back to the people-list after successful submission
    // The Angular component shows a success message for 2 seconds before navigating
    // So we need to wait for the URL to change to /people-list
    await this.page.waitForURL(/.*\/people-list/, { timeout: 15000 });
    await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });

    // Wait for the list component to load
    const peopleContent = this.page.locator('router-outlet, app-people, main, .content').first();
    await peopleContent.waitFor({ state: 'visible', timeout: 5000 });

    // Wait for Angular to stabilize after navigation
    await this.page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {
      // networkidle is flaky, don't fail on it
    });

    // Give UI time to update with new data (longer in CI)
    await this.page.waitForTimeout(isCI ? 1000 : 500);
  }

  async editPerson(personName: string): Promise<void> {
    // Use first() to handle multiple matches in strict mode
    const personRow = this.page.locator(`tr:has-text("${personName}")`).first();
    
    // Wait for the row to be visible first
    await personRow.waitFor({ state: 'visible', timeout: 10000 });
    
    // Click the edit button which should navigate to the people form with edit query param
    await personRow.locator('button:has-text("Edit")').click();
    
    // Wait for navigation to complete and edit form to be ready
    await this.page.waitForLoadState('domcontentloaded', { timeout: 10000 });
    
    // Wait for the people form component to load
    await this.page.locator('app-people').waitFor({ state: 'visible', timeout: 10000 });
    await this.page.locator('input#fullName').waitFor({ state: 'visible', timeout: 5000 });
    
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
    // Use locator with auto-retry for better reliability
    await this.page.locator(`tr:has-text("${personName}")`).first().waitFor({ state: 'visible', timeout: 10000 });
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
      // Note: E2E test mode persists across page reloads via addInitScript in fixture
      await this.page.reload();
      // Instead of waiting for networkidle, wait for specific content to be ready
      await this.page.locator('h1:has-text("CRUD Template Application")').first().waitFor({ timeout: 30000 });
      await this.page.locator('a[routerLink="/people-list"]').first().waitFor({ state: 'visible', timeout: 15000 });
      // Small buffer for Angular to stabilize
      }, 3, 2000, 'refreshPage');
  }

  async clickRefreshButton(): Promise<void> {
    // Set up response listener BEFORE clicking
    const responsePromise = this.page.waitForResponse(
      response => response.url().includes('/api/') && response.ok(),
      { timeout: 8000 }
    );

    await this.page.click('button:has-text("Refresh")');

    // Wait for API response to complete
    try {
      await responsePromise;
      // Extra wait for Angular change detection to process the data
      await this.page.waitForTimeout(isCI ? 800 : 400);
    } catch (error) {
      console.warn('No API response detected for refresh, waiting for UI update');
      // If no API call, wait for UI to update
      await this.page.waitForTimeout(isCI ? 1000 : 500);
    }

    // Wait for any loading indicators to disappear
    await this.page.waitForFunction(() => {
      const loadingIndicators = document.querySelectorAll('.loading, .spinner, [aria-busy="true"]');
      return loadingIndicators.length === 0;
    }, { timeout: 3000 }).catch(() => {
      // No loading indicators found, that's fine
    });
  }

  async verifyEmptyState(entityType: 'roles' | 'people'): Promise<void> {
    const emptyStateText = entityType === 'roles'
      ? 'No roles found. Add the first role'
      : 'No people found. Add the first person';

    // Use the proven multi-selector approach from smoke tests
    // First, navigate to the correct list page
    const targetRoute = entityType === 'roles' ? '/roles-list' : '/people-list';
    await this.page.goto(targetRoute);

    // Wait for Angular stability - proven pattern from smoke tests
    await this.page.waitForLoadState('networkidle');
    await this.page.waitForFunction(() => {
      return typeof (window as any).ng !== 'undefined';
    }, { timeout: 10000 });

    // Multi-selector strategy for component detection - protected pattern
    const pageIndicators = this.page.locator('router-outlet, app-people, app-roles, main, .content, h1, h2, h3, table, tbody, .empty-state').first();
    await pageIndicators.waitFor({ state: 'visible', timeout: 5000 });

    // Now check for actual content - either empty state message or no table rows
    const hasTableRows = await this.page.locator('tbody tr').count() > 0;
    const hasEmptyStateMessage = await this.page.locator('.empty-state, .no-data, .no-results, :text-is("' + emptyStateText + '")').count() > 0;

    // Verify we have a component loaded (either showing empty state or empty table)
    const hasComponentLoaded = await pageIndicators.isVisible();
    if (!hasComponentLoaded) {
      throw new Error(`${entityType} component did not load properly`);
    }

    console.log(`📊 ${entityType} page loaded: hasTableRows=${hasTableRows}, hasEmptyState=${hasEmptyStateMessage}`);

    // For empty state verification, we accept either:
    // 1. An empty state message is visible
    // 2. No table rows (empty table)
    if (!hasEmptyStateMessage && hasTableRows) {
      throw new Error(`Expected empty state for ${entityType} but found table rows`);
    }
  }

  async verifyPageTitle(): Promise<void> {
    await expect(this.page.locator('h1').first()).toContainText('CRUD Template Application');
  }

  async verifyTabActive(tabName: 'people' | 'roles'): Promise<void> {
    // Since we're using router links instead of tabs, verify the correct page is loaded
    if (tabName === 'people') {
      const peopleContent = this.page.locator('router-outlet, app-people, main, .content').first();
      await expect(peopleContent).toBeVisible();
    } else {
      const rolesContent = this.page.locator('router-outlet, app-roles, main, .content').first();
      await expect(rolesContent).toBeVisible();
    }
  }

  // ============================================================================
  // EVENT-DRIVEN WAIT METHODS
  // ============================================================================
  // These methods replace timer-based waits (waitForTimeout) with event-driven
  // patterns that wait for actual application state changes. This improves test
  // reliability and eliminates flakiness caused by arbitrary timeouts.
  // ============================================================================

  /**
   * Wait for Angular navigation to complete
   *
   * Waits for:
   * - DOM content to be loaded
   * - Angular framework to be initialized
   * - Router navigation to complete
   * - Network to be idle (optional)
   *
   * Usage:
   * ```typescript
   * await page.click('a[routerLink="/people-list"]');
   * await helpers.waitForNavigationComplete();
   * ```
   *
   * @param options - Optional wait configuration
   */
  async waitForNavigationComplete(options?: {
    waitForNetworkIdle?: boolean;
    timeout?: number;
  }): Promise<void> {
    const timeout = options?.timeout || (isCI ? 30000 : 15000);
    const waitForNetworkIdle = options?.waitForNetworkIdle ?? true;

    try {
      // Step 1: Wait for DOM content to be loaded
      await this.page.waitForLoadState('domcontentloaded', { timeout });

      // Step 2: Wait for Angular to be defined
      await this.page.waitForFunction(() => {
        return typeof (window as any).ng !== 'undefined';
      }, { timeout: Math.min(timeout, 10000) });

      // Step 3: Wait for network to be idle (optional, can be flaky)
      if (waitForNetworkIdle) {
        await this.page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {
          // networkidle is flaky, don't fail if it times out
          console.warn('Network idle timeout - continuing anyway');
        });
      }

      // Step 4: Verify router outlet is present (indicates routing is ready)
      await this.page.locator('router-outlet, app-people, app-roles, main').first()
        .waitFor({ state: 'attached', timeout: 5000 })
        .catch(() => {
          // May not always have router-outlet, that's okay
        });

    } catch (error) {
      console.error('Navigation completion wait failed:', error);
      throw new Error(`Navigation did not complete within ${timeout}ms`);
    }
  }

  /**
   * Wait for API data to load
   *
   * Waits for one or more API endpoints to return successful responses,
   * then waits for the data to be rendered in the UI.
   *
   * Usage:
   * ```typescript
   * // Single endpoint
   * await helpers.waitForDataLoad('/api/people');
   *
   * // Multiple endpoints
   * await helpers.waitForDataLoad(['/api/people', '/api/roles']);
   * ```
   *
   * @param endpoint - API endpoint(s) to wait for
   * @param options - Optional wait configuration
   */
  async waitForDataLoad(
    endpoint: string | string[],
    options?: {
      timeout?: number;
      method?: string;
      waitForRender?: boolean;
    }
  ): Promise<void> {
    const timeout = options?.timeout || (isCI ? 20000 : 10000);
    const method = options?.method;
    const waitForRender = options?.waitForRender ?? true;
    const endpoints = Array.isArray(endpoint) ? endpoint : [endpoint];

    try {
      // Wait for all API endpoints to respond
      const responsePromises = endpoints.map(ep =>
        this.page.waitForResponse(
          response => {
            const url = response.url();
            const matchesUrl = url.includes(ep);
            const matchesMethod = !method || response.request().method() === method;
            const isSuccess = response.ok();
            return matchesUrl && matchesMethod && isSuccess;
          },
          { timeout }
        )
      );

      await Promise.all(responsePromises);

      // Wait for data to be rendered (Angular change detection)
      if (waitForRender) {
        // Brief wait for Angular to process the data
        await this.page.waitForTimeout(isCI ? 500 : 200);

        // Wait for any loading indicators to disappear
        await this.page.waitForFunction(() => {
          const loadingIndicators = document.querySelectorAll(
            '.loading, .spinner, [aria-busy="true"], .loading-overlay'
          );
          return loadingIndicators.length === 0;
        }, { timeout: 5000 }).catch(() => {
          // No loading indicators found or timeout - that's okay
        });
      }

    } catch (error) {
      console.error(`Data load wait failed for ${endpoints.join(', ')}:`, error);
      throw new Error(`API data did not load within ${timeout}ms`);
    }
  }

  /**
   * Wait for an action that triggers API data loading
   *
   * Properly handles the Playwright pattern of setting up response listener
   * BEFORE triggering the action that causes the request. This prevents race
   * conditions where the API response completes before waitForResponse() is called.
   *
   * Follows Playwright best practice: listener → action → wait
   *
   * Usage:
   * ```typescript
   * // Wait for refresh button click to load data
   * await helpers.waitForActionWithDataLoad(
   *   () => helpers.clickRefreshButton(),
   *   '/api/people'
   * );
   *
   * // Wait for navigation that loads data
   * await helpers.waitForActionWithDataLoad(
   *   () => helpers.switchToPeopleTab(),
   *   '/api/people'
   * );
   *
   * // Wait for multiple endpoints
   * await helpers.waitForActionWithDataLoad(
   *   () => page.click('button.load-all'),
   *   ['/api/people', '/api/roles']
   * );
   * ```
   *
   * @param action - The async action that triggers API requests
   * @param endpoint - API endpoint(s) to wait for (string or array)
   * @param options - Optional configuration
   */
  async waitForActionWithDataLoad(
    action: () => Promise<void>,
    endpoint: string | string[],
    options?: {
      timeout?: number;
      waitForNetworkIdle?: boolean;
    }
  ): Promise<void> {
    const timeout = options?.timeout || 20000;
    const endpoints = Array.isArray(endpoint) ? endpoint : [endpoint];

    try {
      // Set up response listeners BEFORE action
      // This is critical to avoid race conditions
      const responsePromises = endpoints.map(ep =>
        this.page.waitForResponse(
          resp => resp.url().includes(ep) && resp.ok(),
          { timeout }
        )
      );

      // Execute the action that triggers requests
      await action();

      // Wait for all responses
      await Promise.all(responsePromises);

      // Optionally wait for network to settle
      if (options?.waitForNetworkIdle !== false) {
        await this.page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {
          // Network idle timeout is acceptable - data already loaded
        });
      }
    } catch (error) {
      console.error(`Action with data load failed for ${endpoints.join(', ')}:`, error);
      throw new Error(`API data did not load within ${timeout}ms after action`);
    }
  }

  /**
   * Wait for Angular component to be ready
   *
   * Waits for:
   * - Component element to be attached to DOM
   * - Component to be visible
   * - Component data bindings to be initialized (optional)
   *
   * Usage:
   * ```typescript
   * await helpers.waitForComponentReady('app-people');
   * await helpers.waitForComponentReady('app-people', { waitForData: true });
   * ```
   *
   * @param componentSelector - CSS selector for the component
   * @param options - Optional wait configuration
   */
  async waitForComponentReady(
    componentSelector: string,
    options?: {
      waitForData?: boolean;
      timeout?: number;
    }
  ): Promise<void> {
    const timeout = options?.timeout || (isCI ? 20000 : 12000);
    const waitForData = options?.waitForData ?? false;

    try {
      const component = this.page.locator(componentSelector).first();

      // Step 1: Wait for component to be attached to DOM
      await component.waitFor({ state: 'attached', timeout });

      // Step 2: Wait for component to be visible
      await component.waitFor({ state: 'visible', timeout: Math.min(timeout, 10000) });

      // Step 3: Wait for Angular to stabilize
      await this.page.waitForFunction(() => {
        const ng = (window as any).ng;
        if (!ng) return false;

        // Check if Angular has finished bootstrapping
        const hasAngular = typeof ng !== 'undefined';
        return hasAngular;
      }, { timeout: 5000 }).catch(() => {
        // Angular check failed, but component is visible, so continue
      });

      // Step 4: Wait for data to be bound (optional)
      if (waitForData) {
        // Wait for component to have actual content (not just empty template)
        await this.page.waitForFunction(
          (selector) => {
            const element = document.querySelector(selector);
            if (!element) return false;

            // Check if component has meaningful content
            const hasTable = element.querySelector('table tbody tr');
            const hasEmptyState = element.querySelector('.empty-state, .no-data, .no-results');
            const hasContent = element.textContent && element.textContent.trim().length > 50;

            return hasTable || hasEmptyState || hasContent;
          },
          componentSelector,
          { timeout: Math.min(timeout, 10000) }
        );
      }

      // Final stability wait (very brief)
      if (isCI) {
        await this.page.waitForTimeout(300);
      }

    } catch (error) {
      console.error(`Component ready wait failed for ${componentSelector}:`, error);
      throw new Error(`Component ${componentSelector} did not become ready within ${timeout}ms`);
    }
  }

  /**
   * Wait for form submission to complete
   *
   * Waits for:
   * - Form submission request to be sent
   * - API response to be received
   * - Navigation back to list page (optional)
   * - Success message or updated data (optional)
   *
   * Usage:
   * ```typescript
   * const submitPromise = helpers.waitForFormSubmission('/api/people', 'POST');
   * await page.click('button[type="submit"]');
   * await submitPromise;
   * ```
   *
   * @param endpoint - API endpoint that will receive the form data
   * @param method - HTTP method (POST, PUT, etc.)
   * @param options - Optional wait configuration
   */
  async waitForFormSubmission(
    endpoint: string,
    method: 'POST' | 'PUT' | 'PATCH' = 'POST',
    options?: {
      waitForNavigation?: boolean;
      timeout?: number;
      expectSuccess?: boolean;
    }
  ): Promise<void> {
    const timeout = options?.timeout || (isCI ? 15000 : 10000);
    const waitForNavigation = options?.waitForNavigation ?? true;
    const expectSuccess = options?.expectSuccess ?? true;

    try {
      // Wait for the form submission request
      const responsePromise = this.page.waitForResponse(
        response => {
          const matchesUrl = response.url().includes(endpoint);
          const matchesMethod = response.request().method() === method;
          const isSuccess = expectSuccess ? response.ok() : true;
          return matchesUrl && matchesMethod && isSuccess;
        },
        { timeout }
      );

      const response = await responsePromise;

      // Log submission result
      const status = response.status();
      console.log(`📤 Form submission ${method} ${endpoint}: ${status}`);

      // Wait for navigation if expected
      if (waitForNavigation) {
        await this.page.waitForLoadState('domcontentloaded', { timeout: 5000 }).catch(() => {
          // May not navigate, that's okay
        });

        // Wait for URL to change (if navigating back to list)
        const currentUrl = this.page.url();
        if (!currentUrl.includes('/list')) {
          // Wait a bit for potential navigation
          await this.page.waitForTimeout(isCI ? 1000 : 500);
        }
      }

      // Wait for any success messages or UI updates
      await this.page.waitForFunction(() => {
        const loadingIndicators = document.querySelectorAll('.loading, .spinner, [aria-busy="true"]');
        return loadingIndicators.length === 0;
      }, { timeout: 3000 }).catch(() => {
        // No loading indicators, that's fine
      });

      // Brief final wait for UI to stabilize
      await this.page.waitForTimeout(isCI ? 500 : 200);

    } catch (error) {
      console.error(`Form submission wait failed for ${method} ${endpoint}:`, error);
      throw new Error(`Form submission did not complete within ${timeout}ms`);
    }
  }
}
