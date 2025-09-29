import { test, expect } from '../fixtures/serial-test-fixture';
import { PerformanceOptimizedHelpers } from '../helpers/performance-optimized-helpers';

/**
 * E2E Performance Benchmark Tests
 *
 * Validates performance characteristics and establishes benchmarks
 * for critical user operations in Testing configuration.
 */

test.describe('@extended E2E Performance Benchmarks', () => {
  let perfHelpers: PerformanceOptimizedHelpers;

  test.beforeEach(async ({ page, request, apiUrl }) => {
    perfHelpers = new PerformanceOptimizedHelpers(page, request, apiUrl);
  });

  test('@extended Page load performance meets targets', async ({ page, baseURL }) => {
    const startTime = Date.now();

    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    const loadTime = Date.now() - startTime;
    const metrics = await perfHelpers.getPageLoadMetrics();

    // Performance targets for Testing configuration
    expect(loadTime).toBeLessThan(3000); // Initial page load < 3 seconds
    expect(metrics.domContentLoaded).toBeLessThan(2000); // DOM ready < 2 seconds
    expect(metrics.firstContentfulPaint).toBeLessThan(1500); // FCP < 1.5 seconds

    console.log('Page Load Performance:', {
      totalLoadTime: loadTime,
      domContentLoaded: metrics.domContentLoaded,
      firstContentfulPaint: metrics.firstContentfulPaint
    });
  });

  test('@extended Navigation performance across modules', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    // Measure navigation to People
    const peopleNavTime = await perfHelpers.measureOperationTime(
      () => perfHelpers.navigateToModule('people'),
      'Navigate to People'
    );

    // Measure navigation to Roles
    const rolesNavTime = await perfHelpers.measureOperationTime(
      () => perfHelpers.navigateToModule('roles'),
      'Navigate to Roles'
    );

    // Navigation should be fast (< 1 second per module)
    expect(peopleNavTime).toBeLessThan(1000);
    expect(rolesNavTime).toBeLessThan(1000);
  });

  test('@extended API operation performance benchmarks', async ({ page, apiUrl, request }) => {
    // Benchmark individual API operations
    const operations = [
      {
        name: 'GET /api/people',
        operation: () => request.get(`${apiUrl}/api/people`),
        target: 200
      },
      {
        name: 'GET /api/roles',
        operation: () => request.get(`${apiUrl}/api/roles`),
        target: 200
      },
      {
        name: 'POST /api/people',
        operation: () => request.post(`${apiUrl}/api/people`, {
          data: perfHelpers.buildTestPerson()
        }),
        target: 500
      }
    ];

    for (const { name, operation, target } of operations) {
      const time = await perfHelpers.measureOperationTime(operation, name);
      expect(time).toBeLessThan(target);
    }
  });

  test('@extended Bulk data handling performance', async ({ page, request, apiUrl, baseURL }) => {
    const startTime = Date.now();

    // Create multiple people to test bulk performance
    const people = await perfHelpers.createMultiplePeople(5);
    const creationTime = Date.now() - startTime;

    // Navigate to people list and verify all are displayed
    await page.goto(baseURL);
    await perfHelpers.navigateToModule('people');

    // Verify all people appear in UI
    let visibleCount = 0;
    for (const person of people) {
      if (await perfHelpers.validatePersonInList(person.fullName)) {
        visibleCount++;
      }
    }

    expect(visibleCount).toBe(people.length);

    // Cleanup
    await perfHelpers.cleanupMultiplePeople(people.map(p => p.id!));

    // Performance targets
    expect(creationTime).toBeLessThan(3000); // 5 people creation < 3 seconds

    console.log(`Bulk creation of ${people.length} people took ${creationTime}ms`);
  });

  test('@extended Form interaction performance', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await perfHelpers.navigateToModule('people');

    // Measure form opening
    const formOpenTime = await perfHelpers.measureOperationTime(async () => {
      const addButton = page.locator('button:has-text("Add New Person")');
      await addButton.click();
      await page.locator('app-people form').waitFor({ state: 'visible', timeout: 5000 });
    }, 'Open Add Person Form');

    // Measure form filling
    const formData = perfHelpers.buildTestPerson();
    const formFillTime = await perfHelpers.measureOperationTime(
      () => perfHelpers.fillPersonFormFast(formData),
      'Fill Person Form'
    );

    // Performance targets
    expect(formOpenTime).toBeLessThan(1000); // Form opens < 1 second
    expect(formFillTime).toBeLessThan(500); // Form filling < 0.5 seconds

    console.log('Form Performance:', {
      openTime: formOpenTime,
      fillTime: formFillTime
    });
  });

  test.skip('@extended Database operation performance', async ({ page, apiUrl, request }) => {
    // Test database performance with concurrent operations
    const startTime = Date.now();

    const suffixes = ['Alpha', 'Beta', 'Gamma'];
    const concurrentOperations = Array.from({ length: 3 }, async (_, i) => {
      const person = await perfHelpers.createTestPerson({
        fullName: `Concurrent User ${suffixes[i]}`,
        phone: `+1-555-000${i}`
      });

      // Read back immediately
      const response = await request.get(`${apiUrl}/api/people/${person.id}`);
      expect(response.ok()).toBe(true);

      // Cleanup
      await perfHelpers.cleanupTestPerson(person.id!);

      return person;
    });

    await Promise.all(concurrentOperations);
    const totalTime = Date.now() - startTime;

    // Concurrent CRUD operations should complete quickly
    expect(totalTime).toBeLessThan(2000); // 3 concurrent CRUD cycles < 2 seconds

    console.log(`Concurrent database operations took ${totalTime}ms`);
  });
});

test.describe('@critical Performance Regression Detection', () => {
  test.skip('@critical Critical user path performance baseline', async ({ page, baseURL, apiUrl }) => {
    // This test establishes baseline performance for the most critical user path
    const startTime = Date.now();

    // Complete user journey: Load app → Navigate → Create → Verify
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    const perfHelpers = new PerformanceOptimizedHelpers(page, page.request, apiUrl);
    await perfHelpers.navigateToModule('people');

    const person = await perfHelpers.createTestPerson();
    const isVisible = await perfHelpers.validatePersonInList(person.fullName);
    expect(isVisible).toBe(true);

    await perfHelpers.cleanupTestPerson(person.id!);

    const totalTime = Date.now() - startTime;

    // Critical path should complete within reasonable time
    expect(totalTime).toBeLessThan(5000); // Complete critical path < 5 seconds

    console.log(`Critical user path completed in ${totalTime}ms`);
  });

  test('@critical Smoke test execution time benchmark', async ({ page, apiUrl }) => {
    // This test measures how long it takes to run essential smoke tests
    const operations = [
      () => page.request.get(`${apiUrl}/health`),
      () => page.request.get(`${apiUrl}/api/people`),
      () => page.request.get(`${apiUrl}/api/roles`)
    ];

    const startTime = Date.now();

    for (const operation of operations) {
      const response = await operation();
      expect(response.ok()).toBe(true);
    }

    const totalTime = Date.now() - startTime;

    // Essential smoke tests should be very fast
    expect(totalTime).toBeLessThan(1000); // All smoke API calls < 1 second

    console.log(`Smoke test operations completed in ${totalTime}ms`);
  });
});

test.describe('@smoke Performance Monitoring', () => {
  test('@smoke Should track and log performance metrics', async ({ page, baseURL }) => {
    await page.goto(baseURL);
    await page.waitForSelector('h1:has-text("CRUD Template Application")', { timeout: 10000 });

    const perfHelpers = new PerformanceOptimizedHelpers(page, page.request, '');
    const metrics = await perfHelpers.getPageLoadMetrics();

    // Log metrics for monitoring (no assertions, just data collection)
    console.log('Performance Metrics Snapshot:', {
      timestamp: new Date().toISOString(),
      environment: 'Testing',
      metrics: metrics,
      userAgent: await page.evaluate(() => navigator.userAgent)
    });

    // Basic sanity checks
    expect(metrics.domContentLoaded).toBeGreaterThan(0);
    expect(metrics.loadComplete).toBeGreaterThan(0);
  });

  test('@smoke Should detect slow operations', async ({ page, apiUrl }) => {
    const slowOperationThreshold = 2000; // 2 seconds
    const operations = [
      { name: 'Health Check', operation: () => page.request.get(`${apiUrl}/health`) },
      { name: 'People List', operation: () => page.request.get(`${apiUrl}/api/people`) },
      { name: 'Roles List', operation: () => page.request.get(`${apiUrl}/api/roles`) }
    ];

    for (const { name, operation } of operations) {
      const startTime = Date.now();
      const response = await operation();
      const duration = Date.now() - startTime;

      expect(response.ok()).toBe(true);

      if (duration > slowOperationThreshold) {
        console.warn(`Slow operation detected: ${name} took ${duration}ms`);
      }

      // For smoke tests, no operation should be extremely slow
      expect(duration).toBeLessThan(slowOperationThreshold);
    }
  });
});