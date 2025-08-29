import { test, expect } from '@playwright/test';
import { ApiHelpers } from '../helpers/api-helpers';
import * as fs from 'fs/promises';
import * as path from 'path';

/**
 * Integration tests to verify the complete serial execution flow
 * according to ADR-001 and the simplified E2E test strategy
 */
test.describe('Serial Execution Flow Integration', () => {
  let apiHelpers: ApiHelpers;

  test.beforeEach(async ({ request }) => {
    apiHelpers = new ApiHelpers(request, 0);
  });

  test('should verify serial execution configuration', async () => {
    // Verify workers configuration
    expect(test.info().parallelIndex).toBe(0); // Should always be 0 in serial mode
    expect(test.info().workerIndex).toBe(0); // Should always be 0 with single worker
  });

  test('should verify database isolation between tests', async ({ request }) => {
    // Create test data
    const testPerson = {
      fullName: 'Serial Test User',
      phone: '+1-555-0001'
    };
    
    // Create person in this test
    const createResponse = await request.post('/api/people', {
      data: testPerson
    });
    expect(createResponse.ok()).toBe(true);
    const created = await createResponse.json();
    
    // Verify person exists
    const getResponse = await request.get(`/api/people/${created.id}`);
    expect(getResponse.ok()).toBe(true);
    
    // Store ID for next test to verify it doesn't exist
    process.env.TEST_PERSON_ID = created.id;
  });

  test('should have clean database after previous test', async ({ request }) => {
    // Verify the person from previous test doesn't exist
    const previousId = process.env.TEST_PERSON_ID;
    if (previousId) {
      const getResponse = await request.get(`/api/people/${previousId}`);
      expect(getResponse.status()).toBe(404); // Should not exist
    }
    
    // Verify database is empty
    const peopleResponse = await request.get('/api/people');
    const people = await peopleResponse.json();
    expect(people).toHaveLength(0);
  });

  test('should verify server persistence across tests', async ({ request }) => {
    // Servers should stay running throughout the test suite
    // Verify by checking API health endpoint
    const healthResponse = await request.get('/health');
    expect(healthResponse.ok()).toBe(true);
    
    // Verify database path from environment
    const dbPath = process.env.DATABASE_PATH;
    expect(dbPath).toBeTruthy();
    expect(dbPath).toContain('CrudTest_');
    
    // Verify API and Angular URLs are set
    expect(process.env.API_URL).toBe('http://localhost:5172');
    expect(process.env.ANGULAR_URL).toBe('http://localhost:4200');
  });

  test('should handle rapid sequential operations without connection issues', async ({ request }) => {
    // This tests that our database connection pooling fix works
    const operations = [];
    
    // Perform 20 rapid operations (more than the ~15 that used to fail)
    for (let i = 0; i < 20; i++) {
      operations.push(
        request.post('/api/people', {
          data: {
            fullName: `Rapid Test User ${i}`,
            phone: `+1-555-${String(i).padStart(4, '0')}`
          }
        })
      );
    }
    
    // All operations should succeed
    const responses = await Promise.all(operations);
    responses.forEach(response => {
      expect(response.ok()).toBe(true);
    });
    
    // Verify all were created
    const peopleResponse = await request.get('/api/people');
    const people = await peopleResponse.json();
    expect(people.length).toBeGreaterThanOrEqual(20);
  });

  test('should verify database reset performance', async ({ request }) => {
    // Create some test data
    for (let i = 0; i < 10; i++) {
      await request.post('/api/people', {
        data: {
          fullName: `Performance Test User ${i}`,
          phone: `+1-555-${String(i).padStart(4, '0')}`
        }
      });
    }
    
    // Measure database reset time
    const startTime = Date.now();
    const resetResponse = await request.post('/api/database/reset', {
      data: { workerIndex: null } // Nullable as per our fix
    });
    const resetTime = Date.now() - startTime;
    
    expect(resetResponse.ok()).toBe(true);
    
    // Should be fast (under 100ms, typically 2-3ms)
    expect(resetTime).toBeLessThan(100);
    
    // Verify database is empty
    const peopleResponse = await request.get('/api/people');
    const people = await peopleResponse.json();
    expect(people).toHaveLength(0);
  });

  test('should verify no worker complexity remains', async () => {
    // Verify no worker-related files exist
    const testsDir = path.join(__dirname, '..');
    const files = await fs.readdir(testsDir, { recursive: true });
    
    const workerFiles = files.filter(file => 
      file.includes('worker-') && 
      !file.includes('serial-execution-flow.spec.ts')
    );
    
    expect(workerFiles).toHaveLength(0);
  });

  test('should verify simplified configuration structure', async () => {
    // Check that only essential configs exist
    const configDir = path.join(__dirname, '../..');
    const configFiles = await fs.readdir(configDir);
    
    const playwrightConfigs = configFiles.filter(file => 
      file.startsWith('playwright.config') && file.endsWith('.ts')
    );
    
    // Should have main config and a few essential variants
    const expectedConfigs = [
      'playwright.config.ts',
      'playwright.config.fast.ts',
      'playwright.config.ci.ts',
      'playwright.config.serial.ts',
      'playwright.config.local.ts',
      'playwright.config.api-only.ts',
      'playwright.config.integration.ts'
    ];
    
    playwrightConfigs.forEach(config => {
      expect(expectedConfigs).toContain(config);
    });
  });

  test('should complete end-to-end flow without timeouts', async ({ page, request }) => {
    // This is a comprehensive test that would have failed before our fixes
    const apiHelpers = new ApiHelpers(request, 0);
    
    // Step 1: Clean database
    await apiHelpers.cleanupAll(true);
    
    // Step 2: Navigate to app
    await page.goto('http://localhost:4200');
    await page.waitForSelector('h1:has-text("People & Roles Management System")', { timeout: 5000 });
    
    // Step 3: Create data via API
    const roleResponse = await request.post('/api/roles', {
      data: { name: 'Test Role', description: 'Integration test role' }
    });
    expect(roleResponse.ok()).toBe(true);
    
    // Step 4: Create person via UI
    await page.click('button:has-text("Add Person")');
    await page.fill('input[name="fullName"]', 'Integration Test User');
    await page.fill('input[name="phone"]', '+1-555-9999');
    await page.click('button:has-text("Save")');
    
    // Step 5: Verify via API
    const peopleResponse = await request.get('/api/people');
    const people = await peopleResponse.json();
    expect(people.length).toBeGreaterThan(0);
    
    // Step 6: Clean up
    await apiHelpers.cleanupAll(true);
    
    // Step 7: Verify cleanup worked
    const finalResponse = await request.get('/api/people');
    const finalPeople = await finalResponse.json();
    expect(finalPeople).toHaveLength(0);
  });
});