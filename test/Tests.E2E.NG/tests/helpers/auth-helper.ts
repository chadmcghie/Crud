import { APIRequestContext } from '@playwright/test';

/**
 * Authentication helper for E2E tests
 * Handles JWT token creation and management for API requests
 */
export class AuthHelper {
  private token: string | null = null;
  private tokenExpiry: number = 0;

  constructor(private request: APIRequestContext, private apiUrl: string) {}

  /**
   * Register a new test user and get authentication token
   */
  async getAuthToken(email?: string, password?: string): Promise<string> {
    // Check if we have a valid cached token
    if (this.token && Date.now() < this.tokenExpiry) {
      return this.token;
    }

    email = email || `test-${Date.now()}@example.com`;
    password = password || 'Test123!@#';

    try {
      // Register a new user
      const registerResponse = await this.request.post(`${this.apiUrl}/api/auth/register`, {
        data: {
          Email: email,
          Password: password,
          FirstName: 'Test',
          LastName: 'User'
        }
      });

      if (!registerResponse.ok()) {
        throw new Error(`Registration failed: ${registerResponse.status()} ${await registerResponse.text()}`);
      }

      const tokenData = await registerResponse.json();
      this.token = tokenData.accessToken;

      // Set expiry to 50 minutes from now (tokens usually expire in 1 hour)
      this.tokenExpiry = Date.now() + (50 * 60 * 1000);

      return this.token;
    } catch (error) {
      // If registration fails (user might already exist), try login
      try {
        const loginResponse = await this.request.post(`${this.apiUrl}/api/auth/login`, {
          data: {
            Email: email,
            Password: password
          }
        });

        if (!loginResponse.ok()) {
          throw new Error(`Login failed: ${loginResponse.status()} ${await loginResponse.text()}`);
        }

        const tokenData = await loginResponse.json();
        this.token = tokenData.accessToken;
        this.tokenExpiry = Date.now() + (50 * 60 * 1000);

        return this.token;
      } catch (loginError) {
        throw new Error(`Authentication failed - Register: ${error}, Login: ${loginError}`);
      }
    }
  }

  /**
   * Get authorization headers for API requests
   */
  async getAuthHeaders(): Promise<Record<string, string>> {
    const token = await this.getAuthToken();
    return {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    };
  }

  /**
   * Create an authenticated request context
   */
  async createAuthenticatedRequest(): Promise<APIRequestContext> {
    const headers = await this.getAuthHeaders();
    return this.request; // We'll modify the ApiHelpers to use the headers
  }

  /**
   * Clear cached token (useful for testing token refresh)
   */
  clearToken(): void {
    this.token = null;
    this.tokenExpiry = 0;
  }
}