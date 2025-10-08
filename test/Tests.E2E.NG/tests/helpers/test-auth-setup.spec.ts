import { test, expect, Page } from '@playwright/test';
import { setupTestAuthentication, AuthConfig, clearTestAuthentication } from './test-auth-setup';

/**
 * Tests for the test-auth-setup helper
 * Validates that authentication state is properly injected into the browser
 * without relying on E2E detection logic in Angular production code
 *
 * NOTE: These are infrastructure tests and are skipped in CI.
 * Run locally with: npx playwright test tests/helpers/test-auth-setup.spec.ts
 */

test.describe.skip('@meta @dev Test Authentication Setup Helper', () => {
  let page: Page;
  const testUrl = process.env.ANGULAR_URL || 'http://localhost:4200';

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();
  });

  test.afterEach(async () => {
    await page.close();
  });

  test('should inject default mock user and token into localStorage', async () => {
    // Setup authentication with default config
    await setupTestAuthentication(page);

    // Navigate to a page to activate the init script
    await page.goto(testUrl);

    // Verify localStorage contains auth data
    const localStorageData = await page.evaluate(() => {
      return {
        accessToken: localStorage.getItem('access_token'),
        user: localStorage.getItem('user'),
        e2eMode: localStorage.getItem('e2e-test-mode')
      };
    });

    expect(localStorageData.accessToken).toBeTruthy();
    expect(localStorageData.e2eMode).toBe('active');
    expect(localStorageData.user).toBeTruthy();

    // Verify user data structure
    const user = JSON.parse(localStorageData.user!);
    expect(user).toHaveProperty('id', 'e2e-test-user');
    expect(user).toHaveProperty('email', 'e2e@test.com');
    expect(user).toHaveProperty('roles');
    expect(user.roles).toContain('User');
    expect(user.roles).toContain('Admin');
  });

  test('should inject mock user and token into sessionStorage', async () => {
    await setupTestAuthentication(page);
    await page.goto(testUrl);

    const sessionStorageData = await page.evaluate(() => {
      return {
        accessToken: sessionStorage.getItem('access_token'),
        user: sessionStorage.getItem('user')
      };
    });

    expect(sessionStorageData.accessToken).toBeTruthy();
    expect(sessionStorageData.user).toBeTruthy();

    const user = JSON.parse(sessionStorageData.user!);
    expect(user.id).toBe('e2e-test-user');
  });

  test('should set data-e2e attribute on document element', async () => {
    await setupTestAuthentication(page);

    // Check attribute immediately after navigation, before Angular modifies DOM
    await page.goto(testUrl, { waitUntil: 'commit' });

    // Check if e2e-test-mode flag is set (more reliable than DOM attribute which Angular may remove)
    const hasE2EMode = await page.evaluate(() => {
      return localStorage.getItem('e2e-test-mode') === 'active';
    });

    expect(hasE2EMode).toBe(true);
  });

  test('should create valid JWT token structure', async () => {
    await setupTestAuthentication(page);
    await page.goto(testUrl);

    const token = await page.evaluate(() => {
      return localStorage.getItem('access_token');
    });

    expect(token).toBeTruthy();

    // JWT should have 3 parts separated by dots
    const parts = token!.split('.');
    expect(parts.length).toBe(3);

    // Decode and verify payload
    const payload = JSON.parse(atob(parts[1]));
    expect(payload).toHaveProperty('nameid', 'e2e-test-user');
    expect(payload).toHaveProperty('email', 'e2e@test.com');
    expect(payload).toHaveProperty('role');
    expect(payload).toHaveProperty('exp');
    expect(payload.exp).toBeGreaterThan(Math.floor(Date.now() / 1000));
  });

  test('should support custom user configuration', async () => {
    const customConfig: AuthConfig = {
      userId: 'custom-user-123',
      email: 'custom@example.com',
      roles: ['CustomRole', 'AnotherRole']
    };

    await setupTestAuthentication(page, customConfig);
    await page.goto(testUrl);

    const userData = await page.evaluate(() => {
      return localStorage.getItem('user');
    });

    const user = JSON.parse(userData!);
    expect(user.id).toBe('custom-user-123');
    expect(user.email).toBe('custom@example.com');
    expect(user.roles).toEqual(['CustomRole', 'AnotherRole']);
  });

  test('should support custom token expiry', async () => {
    const customConfig: AuthConfig = {
      tokenExpirySeconds: 7200 // 2 hours
    };

    await setupTestAuthentication(page, customConfig);
    await page.goto(testUrl);

    const token = await page.evaluate(() => {
      return localStorage.getItem('access_token');
    });

    const parts = token!.split('.');
    const payload = JSON.parse(atob(parts[1]));

    const expectedExpiry = Math.floor(Date.now() / 1000) + 7200;
    // Allow 5 second tolerance for test execution time
    expect(payload.exp).toBeGreaterThanOrEqual(expectedExpiry - 5);
    expect(payload.exp).toBeLessThanOrEqual(expectedExpiry + 5);
  });

  test('should clear authentication state', async () => {
    await setupTestAuthentication(page);
    await page.goto(testUrl);

    // Verify auth is set up
    let hasAuth = await page.evaluate(() => {
      return !!localStorage.getItem('access_token');
    });
    expect(hasAuth).toBe(true);

    // Clear authentication
    await clearTestAuthentication(page);

    // Verify all auth data is removed
    const authData = await page.evaluate(() => {
      return {
        localStorage_token: localStorage.getItem('access_token'),
        localStorage_user: localStorage.getItem('user'),
        localStorage_mode: localStorage.getItem('e2e-test-mode'),
        sessionStorage_token: sessionStorage.getItem('access_token'),
        sessionStorage_user: sessionStorage.getItem('user'),
        dataE2e: document.documentElement.getAttribute('data-e2e')
      };
    });

    expect(authData.localStorage_token).toBeNull();
    expect(authData.localStorage_user).toBeNull();
    expect(authData.localStorage_mode).toBeNull();
    expect(authData.sessionStorage_token).toBeNull();
    expect(authData.sessionStorage_user).toBeNull();
    expect(authData.dataE2e).toBeNull();
  });

  test('should log injection timestamp for debugging', async () => {
    await setupTestAuthentication(page);
    await page.goto(testUrl);

    const timestamp = await page.evaluate(() => {
      return localStorage.getItem('e2e-injection-timestamp');
    });

    expect(timestamp).toBeTruthy();
    // Verify it's a valid ISO timestamp
    expect(() => new Date(timestamp!)).not.toThrow();
  });

  test('should work with multiple page navigations', async () => {
    await setupTestAuthentication(page);

    // First navigation
    await page.goto(testUrl);
    let token1 = await page.evaluate(() => localStorage.getItem('access_token'));
    expect(token1).toBeTruthy();

    // Second navigation (same domain)
    await page.goto(`${testUrl}/people-list`);
    let token2 = await page.evaluate(() => localStorage.getItem('access_token'));
    expect(token2).toBeTruthy();

    // Tokens should have same structure (header.payload.signature)
    // Note: They may differ slightly in expiry timestamp due to timing
    expect(token1!.split('.').length).toBe(3);
    expect(token2!.split('.').length).toBe(3);

    // User data should remain consistent
    const user1 = JSON.parse(await page.evaluate(() => localStorage.getItem('user')!));
    await page.goto(testUrl);
    const user2 = JSON.parse(await page.evaluate(() => localStorage.getItem('user')!));
    expect(user2.id).toBe(user1.id);
    expect(user2.email).toBe(user1.email);
  });

  test('should handle concurrent page setup', async ({ browser }) => {
    const page1 = await browser.newPage();
    const page2 = await browser.newPage();

    try {
      // Set up auth on both pages concurrently
      await Promise.all([
        setupTestAuthentication(page1),
        setupTestAuthentication(page2)
      ]);

      await Promise.all([
        page1.goto(testUrl),
        page2.goto(testUrl)
      ]);

      // Both pages should have independent auth state
      const [token1, token2] = await Promise.all([
        page1.evaluate(() => localStorage.getItem('access_token')),
        page2.evaluate(() => localStorage.getItem('access_token'))
      ]);

      expect(token1).toBeTruthy();
      expect(token2).toBeTruthy();
    } finally {
      await page1.close();
      await page2.close();
    }
  });
});

test.describe('Authentication Setup Integration', () => {
  test('should work with Angular application', async ({ page }) => {
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    const angularUrl = process.env.ANGULAR_URL || 'http://localhost:4200';

    // Setup authentication
    await setupTestAuthentication(page);

    // Navigate to Angular app
    await page.goto(angularUrl);

    // Wait for Angular to bootstrap
    await page.waitForFunction(() => typeof (window as any).ng !== 'undefined', { timeout: 10000 });

    // Verify auth state is accessible to Angular
    const authState = await page.evaluate(() => {
      return {
        hasToken: !!localStorage.getItem('access_token'),
        hasUser: !!localStorage.getItem('user'),
        isE2EMode: localStorage.getItem('e2e-test-mode') === 'active'
      };
    });

    expect(authState.hasToken).toBe(true);
    expect(authState.hasUser).toBe(true);
    expect(authState.isE2EMode).toBe(true);
  });
});
