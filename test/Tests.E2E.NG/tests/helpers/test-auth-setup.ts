import { Page } from '@playwright/test';

/**
 * Configuration options for test authentication setup
 */
export interface AuthConfig {
  /** User ID for the mock authenticated user (default: 'e2e-test-user') */
  userId?: string;
  /** Email address for the mock user (default: 'e2e@test.com') */
  email?: string;
  /** Roles assigned to the mock user (default: ['User', 'Admin']) */
  roles?: string[];
  /** Token expiry time in seconds (default: 3600 = 1 hour) */
  tokenExpirySeconds?: number;
  /** Additional custom claims to include in the JWT payload */
  customClaims?: Record<string, any>;
}

/**
 * Default authentication configuration
 */
const DEFAULT_AUTH_CONFIG: Required<Omit<AuthConfig, 'customClaims'>> = {
  userId: 'e2e-test-user',
  email: 'e2e@test.com',
  roles: ['User', 'Admin'],
  tokenExpirySeconds: 3600
};

/**
 * Sets up test authentication for E2E tests using Playwright's page.addInitScript()
 *
 * This replaces the E2E detection logic previously embedded in Angular's auth.service.ts.
 * Instead of having Angular detect and auto-authenticate E2E tests, we inject the
 * authentication state directly into the browser context before Angular loads.
 *
 * Benefits:
 * - Zero test-specific code in Angular production files
 * - More reliable authentication setup (no race conditions)
 * - Easier to debug and maintain
 * - Better security (no test backdoors in production code)
 *
 * Usage:
 * ```typescript
 * import { setupTestAuthentication } from './helpers/test-auth-setup';
 *
 * test.beforeEach(async ({ page }) => {
 *   await setupTestAuthentication(page);
 *   // Now navigate to your app - auth is already set up
 *   await page.goto('/');
 * });
 * ```
 *
 * @param page - Playwright Page instance
 * @param config - Optional configuration to override defaults
 */
export async function setupTestAuthentication(
  page: Page,
  config: AuthConfig = {}
): Promise<void> {
  // Merge config with defaults
  const authConfig = {
    ...DEFAULT_AUTH_CONFIG,
    ...config
  };

  // Use addInitScript to inject auth state BEFORE any page loads
  // This ensures Angular sees the auth tokens from the very beginning
  await page.addInitScript((configData) => {
    const injectionTimestamp = new Date().toISOString();
    console.log('🔍 STORAGE INJECTION RUNNING - Timestamp:', injectionTimestamp);

    // Create mock authenticated user
    const mockUser = {
      id: configData.userId,
      email: configData.email,
      roles: configData.roles
    };

    // Create mock JWT token with proper structure
    const mockTokenHeader = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));

    const tokenPayload = {
      nameid: mockUser.id,
      email: mockUser.email,
      role: mockUser.roles,
      exp: Math.floor(Date.now() / 1000) + configData.tokenExpirySeconds,
      ...configData.customClaims
    };
    const mockTokenPayload = btoa(JSON.stringify(tokenPayload));
    const mockToken = `${mockTokenHeader}.${mockTokenPayload}.mock-signature`;

    // Set authentication state in both localStorage and sessionStorage
    // This ensures auth persists regardless of "Remember Me" setting
    localStorage.setItem('access_token', mockToken);
    localStorage.setItem('user', JSON.stringify(mockUser));
    sessionStorage.setItem('access_token', mockToken);
    sessionStorage.setItem('user', JSON.stringify(mockUser));

    // Set E2E test markers for additional detection (if still needed during transition)
    localStorage.setItem('e2e-test-mode', 'active');
    localStorage.setItem('e2e-injection-timestamp', injectionTimestamp);
    document.documentElement.setAttribute('data-e2e', 'true');

    console.log('🔍 STORAGE INJECTION COMPLETE:', {
      timestamp: injectionTimestamp,
      mockUser: mockUser,
      tokenLength: mockToken.length,
      localStorage_set: !!localStorage.getItem('access_token'),
      sessionStorage_set: !!sessionStorage.getItem('access_token'),
      e2e_mode_set: localStorage.getItem('e2e-test-mode'),
      document_attr_set: document.documentElement.getAttribute('data-e2e')
    });
  }, authConfig);
}

/**
 * Clears test authentication state from the page
 *
 * This should be called in afterEach hooks to clean up authentication
 * state between tests and prevent state leakage.
 *
 * Usage:
 * ```typescript
 * import { clearTestAuthentication } from './helpers/test-auth-setup';
 *
 * test.afterEach(async ({ page }) => {
 *   await clearTestAuthentication(page);
 * });
 * ```
 *
 * @param page - Playwright Page instance
 */
export async function clearTestAuthentication(page: Page): Promise<void> {
  try {
    await page.evaluate(() => {
      if (typeof localStorage !== 'undefined') {
        // Remove auth tokens
        localStorage.removeItem('e2e-test-mode');
        localStorage.removeItem('e2e-injection-timestamp');
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('user');

        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('refresh_token');
        sessionStorage.removeItem('user');

        // Remove E2E markers
        document.documentElement.removeAttribute('data-e2e');
      }
    });
  } catch (error) {
    // Ignore errors if page wasn't navigated to a valid domain
    // This can happen if the test failed before navigation
    console.warn('Could not clear test authentication:', error);
  }
}

/**
 * Sets up route mocking for authentication endpoints
 *
 * This allows tests to mock auth API responses without hitting the real backend.
 * Useful for testing auth flows, error handling, and edge cases.
 *
 * Usage:
 * ```typescript
 * import { setupAuthRouteMocking } from './helpers/test-auth-setup';
 *
 * test('should handle login', async ({ page }) => {
 *   await setupAuthRouteMocking(page, {
 *     login: { success: true, user: { id: '123', email: 'test@example.com' } }
 *   });
 *   await page.goto('/login');
 *   // Login API calls will be mocked
 * });
 * ```
 *
 * @param page - Playwright Page instance
 * @param options - Route mocking configuration
 */
export interface AuthRouteMockOptions {
  /** Mock login endpoint */
  login?: {
    success: boolean;
    user?: { id: string; email: string; roles?: string[] };
    error?: string;
    status?: number;
  };
  /** Mock register endpoint */
  register?: {
    success: boolean;
    user?: { id: string; email: string; roles?: string[] };
    error?: string;
    status?: number;
  };
  /** Mock refresh token endpoint */
  refresh?: {
    success: boolean;
    error?: string;
    status?: number;
  };
  /** Mock logout endpoint */
  logout?: {
    success: boolean;
    error?: string;
    status?: number;
  };
}

export async function setupAuthRouteMocking(
  page: Page,
  options: AuthRouteMockOptions
): Promise<void> {
  const apiUrl = process.env.API_URL || 'http://localhost:5172';

  // Mock login endpoint
  if (options.login) {
    await page.route(`${apiUrl}/api/auth/login`, async (route) => {
      if (options.login!.success) {
        const mockUser = options.login!.user || {
          id: 'mock-user-id',
          email: 'mock@example.com',
          roles: ['User']
        };

        const mockToken = generateMockToken(mockUser);

        await route.fulfill({
          status: options.login!.status || 200,
          contentType: 'application/json',
          body: JSON.stringify({
            accessToken: mockToken,
            refreshToken: 'mock-refresh-token',
            user: mockUser
          })
        });
      } else {
        await route.fulfill({
          status: options.login!.status || 401,
          contentType: 'application/json',
          body: JSON.stringify({
            error: options.login!.error || 'Invalid credentials'
          })
        });
      }
    });
  }

  // Mock register endpoint
  if (options.register) {
    await page.route(`${apiUrl}/api/auth/register`, async (route) => {
      if (options.register!.success) {
        const mockUser = options.register!.user || {
          id: 'new-user-id',
          email: 'newuser@example.com',
          roles: ['User']
        };

        const mockToken = generateMockToken(mockUser);

        await route.fulfill({
          status: options.register!.status || 201,
          contentType: 'application/json',
          body: JSON.stringify({
            accessToken: mockToken,
            refreshToken: 'mock-refresh-token',
            user: mockUser
          })
        });
      } else {
        await route.fulfill({
          status: options.register!.status || 400,
          contentType: 'application/json',
          body: JSON.stringify({
            error: options.register!.error || 'Registration failed'
          })
        });
      }
    });
  }

  // Mock refresh token endpoint
  if (options.refresh) {
    await page.route(`${apiUrl}/api/auth/refresh`, async (route) => {
      if (options.refresh!.success) {
        await route.fulfill({
          status: options.refresh!.status || 200,
          contentType: 'application/json',
          body: JSON.stringify({
            accessToken: 'new-mock-access-token',
            refreshToken: 'new-mock-refresh-token'
          })
        });
      } else {
        await route.fulfill({
          status: options.refresh!.status || 401,
          contentType: 'application/json',
          body: JSON.stringify({
            error: options.refresh!.error || 'Invalid refresh token'
          })
        });
      }
    });
  }

  // Mock logout endpoint
  if (options.logout) {
    await page.route(`${apiUrl}/api/auth/logout`, async (route) => {
      if (options.logout!.success) {
        await route.fulfill({
          status: options.logout!.status || 200,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Logged out successfully' })
        });
      } else {
        await route.fulfill({
          status: options.logout!.status || 500,
          contentType: 'application/json',
          body: JSON.stringify({
            error: options.logout!.error || 'Logout failed'
          })
        });
      }
    });
  }
}

/**
 * Helper function to generate a mock JWT token
 * @internal
 */
function generateMockToken(user: { id: string; email: string; roles?: string[] }): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = btoa(JSON.stringify({
    nameid: user.id,
    email: user.email,
    role: user.roles || ['User'],
    exp: Math.floor(Date.now() / 1000) + 3600
  }));
  return `${header}.${payload}.mock-signature`;
}

/**
 * Helper to verify authentication state is properly set up
 * Useful for debugging authentication issues in tests
 *
 * @param page - Playwright Page instance
 * @returns Object with authentication state details
 */
export async function verifyAuthSetup(page: Page): Promise<{
  hasAccessToken: boolean;
  hasUser: boolean;
  isE2EMode: boolean;
  userDetails: any;
  tokenPayload: any;
}> {
  return await page.evaluate(() => {
    const accessToken = localStorage.getItem('access_token');
    const userStr = localStorage.getItem('user');
    const e2eMode = localStorage.getItem('e2e-test-mode');

    let tokenPayload = null;
    if (accessToken) {
      try {
        const parts = accessToken.split('.');
        if (parts.length === 3) {
          tokenPayload = JSON.parse(atob(parts[1]));
        }
      } catch (e) {
        console.error('Failed to decode token:', e);
      }
    }

    return {
      hasAccessToken: !!accessToken,
      hasUser: !!userStr,
      isE2EMode: e2eMode === 'active',
      userDetails: userStr ? JSON.parse(userStr) : null,
      tokenPayload
    };
  });
}
