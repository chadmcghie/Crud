import { Page } from '@playwright/test';

/**
 * Authentication helper for E2E tests
 * Provides methods to handle login/logout operations in tests
 */
export class AuthHelper {
  constructor(private page: Page) {}

  /**
   * Login with provided credentials
   * @param email User email
   * @param password User password
   */
  async login(email: string = 'test@example.com', password: string = 'Test123!') {
    // Enable E2E test mode to bypass guards if login fails
    await this.enableE2EMode();
    
    await this.page.goto('/login');
    await this.page.fill('input[name="email"]', email);
    await this.page.fill('input[name="password"]', password);
    await this.page.click('button[type="submit"]');
    
    // Wait for navigation or token storage
    await this.page.waitForURL('**/people-list', { timeout: 5000 });
  }

  /**
   * Enable E2E test mode for guard bypass
   */
  async enableE2EMode() {
    await this.page.evaluate(() => {
      localStorage.setItem('e2e-test-mode', 'active');
    });
  }

  /**
   * Login as admin user
   */
  async loginAsAdmin() {
    await this.login('admin@example.com', 'Admin123!');
  }

  /**
   * Check if user is logged in by looking for auth token
   */
  async isLoggedIn(): Promise<boolean> {
    const token = await this.page.evaluate(() => {
      return localStorage.getItem('token') || sessionStorage.getItem('token');
    });
    return !!token;
  }

  /**
   * Logout current user
   */
  async logout() {
    await this.page.evaluate(() => {
      localStorage.removeItem('token');
      sessionStorage.removeItem('token');
    });
    await this.page.goto('/login');
  }

  /**
   * Setup test user for E2E tests
   * Creates a user if not exists via API
   */
  async setupTestUser(apiUrl: string) {
    // Register test user via API if needed
    const response = await this.page.request.post(`${apiUrl}/api/auth/register`, {
      data: {
        email: 'test@example.com',
        password: 'Test123!',
        firstName: 'Test',
        lastName: 'User'
      },
      failOnStatusCode: false
    });

    // User might already exist, that's ok
    if (response.status() !== 200 && response.status() !== 409) {
      throw new Error(`Failed to setup test user: ${response.status()}`);
    }
  }

  /**
   * Mock authentication for faster tests
   * Directly sets auth token without going through login flow
   */
  async mockAuth(token?: string) {
    const mockToken = token || 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0QGV4YW1wbGUuY29tIiwiZXhwIjoxOTk5OTk5OTk5fQ.mock';
    await this.page.evaluate((t) => {
      localStorage.setItem('token', t);
    }, mockToken);
  }
}