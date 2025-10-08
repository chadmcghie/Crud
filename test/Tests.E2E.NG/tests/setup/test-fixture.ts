import { test as base, APIRequestContext } from '@playwright/test';
import { setupTestAuthentication, clearTestAuthentication } from '../helpers/test-auth-setup';

export interface TestFixtures {
  apiContext: APIRequestContext;
  cleanDatabase: void;
  apiUrl: string;
  angularUrl: string;
}

// Manual cleanup function as fallback
async function manualCleanup(apiContext: APIRequestContext) {
  try {
    // Delete all people first (they reference roles)
    const peopleResponse = await apiContext.get('/api/people', { timeout: 2000 });
    if (peopleResponse.ok()) {
      const people = await peopleResponse.json();
      // Delete in parallel for speed but with error handling
      await Promise.allSettled(
        people.map((person: any) =>
          apiContext.delete(`/api/people/${person.id}`, { timeout: 1000 })
        )
      );
    }

    // Small delay to ensure deletions are processed
    await new Promise(resolve => setTimeout(resolve, 100));

    // Delete all roles after people are deleted
    const rolesResponse = await apiContext.get('/api/roles', { timeout: 2000 });
    if (rolesResponse.ok()) {
      const roles = await rolesResponse.json();
      // Delete in parallel for speed but with error handling
      await Promise.allSettled(
        roles.map((role: any) =>
          apiContext.delete(`/api/roles/${role.id}`, { timeout: 1000 })
        )
      );
    }
  } catch (error) {
    console.warn('Manual cleanup error (will retry):', error);
  }
}

// Simple test fixtures that use environment variables from global setup
export const test = base.extend<TestFixtures>({
  apiUrl: async ({ }, use) => {
    const apiUrl = process.env.API_URL || 'http://localhost:5172';
    await use(apiUrl);
  },

  angularUrl: async ({ }, use) => {
    const angularUrl = process.env.ANGULAR_URL || 'http://localhost:4200';
    await use(angularUrl);
  },

  apiContext: async ({ playwright, apiUrl }, use) => {
    const context = await playwright.request.newContext({
      baseURL: apiUrl,
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });
    await use(context);
    await context.dispose();
  },

  cleanDatabase: [async ({ apiContext }, use, testInfo) => {
    console.log(`🧹 Pre-test cleanup for: ${testInfo.title}`);

    let cleanupAttempted = false;

    // Attempt database cleanup using reset endpoint
    try {
      const resetResponse = await apiContext.post('/api/database/reset', {
        data: {
          workerIndex: 0,
          preserveSchema: true
        },
        headers: {
          'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
        },
        timeout: 5000
      });

      if (resetResponse.ok()) {
        cleanupAttempted = true;
      } else {
        console.warn(`⚠️ Reset endpoint failed with status ${resetResponse.status()}`);
      }
    } catch (error: any) {
      if (error.message?.includes('Timeout')) {
        console.warn('⚠️ Database reset timed out - API may be under load');
      } else {
        console.warn('⚠️ Database reset error:', error.message || error);
      }
    }

    // If reset endpoint failed, fall back to manual cleanup
    if (!cleanupAttempted) {
      console.log('📋 Falling back to manual cleanup...');
      await manualCleanup(apiContext);
    }

    // CRITICAL: Verify cleanup using the database status endpoint
    // This endpoint uses IgnoreQueryFilters() to count ALL entities including soft-deleted ones
    try {
      // Increase timeout and add retry logic for verification
      let statusResp;
      let verifyRetries = 0;
      while (verifyRetries < 3) {
        try {
          statusResp = await apiContext.get('/api/database/status', { timeout: 5000 });
          if (statusResp.ok()) {
            break;
          }
          // If status endpoint returns error, retry
          await new Promise(resolve => setTimeout(resolve, 500));
          verifyRetries++;
        } catch (error: any) {
          if (verifyRetries < 2 && error.message?.includes('Timeout')) {
            console.warn('⏳ Verification timeout, retrying...');
            await new Promise(resolve => setTimeout(resolve, 500));
            verifyRetries++;
          } else {
            throw error;
          }
        }
      }

      if (!statusResp || !statusResp.ok()) {
        throw new Error('Failed to verify database state after cleanup');
      }

      const status = await statusResp.json();

      // Check if database actually has data (including soft-deleted records)
      const hasData = status.peopleCount > 0 || status.rolesCount > 0 ||
                     status.wallsCount > 0 || status.windowsCount > 0 ||
                     status.usersCount > 0;

      if (hasData) {
        console.error(`❌ CLEANUP FAILED! Database still contains data:`);
        console.error(`   People: ${status.peopleCount}, Roles: ${status.rolesCount}`);
        console.error(`   Walls: ${status.wallsCount}, Windows: ${status.windowsCount}`);
        console.error(`   Users: ${status.usersCount}`);

        // Last resort: retry reset endpoint multiple times
        console.log('🔧 Retrying database reset...');

        for (let attempt = 0; attempt < 3; attempt++) {
          try {
            const retryResetResponse = await apiContext.post('/api/database/reset', {
              data: {
                workerIndex: 0,
                preserveSchema: true
              },
              headers: {
                'X-Test-Reset-Token': process.env.TEST_RESET_TOKEN || 'test-only-token'
              },
              timeout: 10000  // Longer timeout for retry
            });

            if (retryResetResponse.ok()) {
              // Wait a bit for the reset to complete
              await new Promise(resolve => setTimeout(resolve, 500));

              // Check if reset worked
              const checkStatusResp = await apiContext.get('/api/database/status', { timeout: 5000 });
              if (checkStatusResp.ok()) {
                const checkStatus = await checkStatusResp.json();
                const stillHasData = checkStatus.peopleCount > 0 || checkStatus.rolesCount > 0 ||
                                   checkStatus.wallsCount > 0 || checkStatus.windowsCount > 0 ||
                                   checkStatus.usersCount > 0;

                if (!stillHasData) {
                  console.log(`✅ Database reset successful on retry attempt ${attempt + 1}`);
                  break;
                }

                if (attempt === 2) {
                  console.error(`⚠️ Database cleanup incomplete after 3 retry attempts.`);
                  console.error(`   People: ${checkStatus.peopleCount}, Roles: ${checkStatus.rolesCount}`);
                  console.error(`   Walls: ${checkStatus.wallsCount}, Windows: ${checkStatus.windowsCount}`);
                  console.error(`   Users: ${checkStatus.usersCount}`);
                  console.error('   Test may fail due to leftover data. Consider restarting the test server.');
                }
              }
            }
          } catch (retryError: any) {
            console.error(`❌ Retry attempt ${attempt + 1} failed:`, retryError.message);
            if (attempt === 2) {
              throw new Error(`Database cleanup failed after 3 retry attempts: ${retryError.message}`);
            }
          }
        }
      } else {
        console.log('✅ Database verified clean - All counts: 0');
      }
    } catch (error: any) {
      console.error('❌ Failed to verify database cleanup:', error.message);
      throw error;
    }

    console.log('🧪 Starting test - database automatically cleaned');
    await use();
  }, { auto: true }],

  // Override page to use Angular URL
  page: async ({ browser, angularUrl }, use) => {
    const context = await browser.newContext();
    const page = await context.newPage();

    // Set timeouts
    page.setDefaultNavigationTimeout(45000);
    page.setDefaultTimeout(15000);

    // Setup test authentication using the dedicated helper
    await setupTestAuthentication(page, {
      userId: 'e2e-test-user',
      email: 'e2e@test.com',
      roles: ['User', 'Admin'],
      useLocalStorage: true
    });

    // NOTE: Do NOT navigate here - let individual tests navigate via pageHelpers.navigateToApp()
    // This ensures addInitScript runs on the actual test navigation, not a premature one

    await use(page);

    // Clean up test authentication
    await clearTestAuthentication(page);

    await context.close();
  },
});

export { expect } from '@playwright/test';
