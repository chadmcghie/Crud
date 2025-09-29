import { chromium, FullConfig } from '@playwright/test';

/**
 * Global setup for E2E tests
 * Creates test users before test execution
 */
async function globalSetup(config: FullConfig) {
  console.log('🔧 Global Setup: Starting...');

  const apiUrl = 'http://localhost:5172';
  const browser = await chromium.launch();
  const page = await browser.newPage();

  try {
    // Wait for API to be ready
    console.log('⏳ Waiting for API to be ready...');
    await page.waitForTimeout(5000); // Give servers time to start

    // Create test users
    const testUsers = [
      {
        email: 'test@example.com',
        password: 'Test123!',
        firstName: 'Test',
        lastName: 'User',
        role: 'User'
      },
      {
        email: 'admin@example.com',
        password: 'Admin123!',
        firstName: 'Admin',
        lastName: 'User',
        role: 'Admin'
      },
      {
        email: 'user@example.com',
        password: 'User123!',
        firstName: 'Regular',
        lastName: 'User',
        role: 'User'
      }
    ];

    for (const user of testUsers) {
      console.log(`👤 Creating test user: ${user.email}`);

      try {
        const response = await page.request.post(`${apiUrl}/api/auth/register`, {
          data: user,
          failOnStatusCode: false,
          headers: {
            'X-E2E-Test': 'true',
            'X-Test-Bypass-Auth': 'true',
          }
        });

        if (response.ok()) {
          console.log(`✅ Created user: ${user.email}`);
        } else if (response.status() === 409) {
          console.log(`ℹ️  User already exists: ${user.email}`);
        } else {
          const errorText = await response.text();
          console.warn(`⚠️  Failed to create user ${user.email}: ${response.status()} - ${errorText}`);
        }
      } catch (error) {
        console.error(`❌ Error creating user ${user.email}:`, error.message);
      }
    }

    console.log('✅ Global Setup: Complete');
  } catch (error) {
    console.error('❌ Global Setup: Failed', error);
    throw error;
  } finally {
    await browser.close();
  }
}

export default globalSetup;