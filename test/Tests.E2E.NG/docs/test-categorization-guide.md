# E2E Test Categorization Guide

## Purpose

This guide defines the criteria for categorizing E2E tests with tags (`@smoke`, `@critical`, `@extended`, `@meta`, `@performance`). Consistent categorization ensures efficient test execution in CI/CD pipelines and clear communication about test coverage.

---

## Test Category Definitions

### @smoke (~2-3 min, run on every PR)

**Purpose**: "Is the application alive and accessible?"

**Criteria**:
- Application starts and loads successfully
- All major API endpoints return 200 OK (basic availability check)
- Navigation elements render correctly
- Seed data loads properly
- Health checks pass
- Basic routing works (can reach main pages)

**Rule of Thumb**: *"If this fails, nothing else will work"*

**Examples**:
```typescript
test('@smoke API server is running', async ({ request, apiUrl }) => {
  const response = await request.get(`${apiUrl}/health`);
  expect(response.ok()).toBe(true);
});

test('@smoke Angular application loads', async ({ page, baseURL }) => {
  await page.goto(baseURL);
  await expect(page.locator('h1')).toBeVisible();
});

test('@smoke Can navigate to people list', async ({ page }) => {
  await page.goto('/people-list');
  await expect(page).toHaveURL(/people-list/);
});
```

**When to Use**:
- ✅ Application loads
- ✅ API endpoints respond (GET /api/people returns 200)
- ✅ Navigation links visible and clickable
- ✅ Page routing works
- ❌ NOT for CRUD operations (use @critical)
- ❌ NOT for validation logic (use @critical)

---

### @critical (~5-10 min, run on every PR)

**Purpose**: "Can users accomplish their primary tasks?"

**Criteria**:
- Complete CRUD operations for each core entity (Person, Role, etc.)
- Required field validation
- Basic user workflows (create → view → edit → delete)
- Navigation between major application sections
- Empty states display correctly
- Data persistence verified

**Rule of Thumb**: *"If this fails, users can't do their job"*

**Examples**:
```typescript
test('@critical should create a new person successfully', async ({ page }) => {
  await pageHelpers.clickAddPerson();
  await pageHelpers.fillPersonForm('John Doe', '+1-555-0123');
  await pageHelpers.submitPersonForm();
  await pageHelpers.verifyPersonExists('John Doe');
});

test('@critical should validate required fields', async ({ page }) => {
  await pageHelpers.clickAddPerson();
  await pageHelpers.verifySubmitButtonDisabled(); // Can't submit without required fields
});

test('@critical should delete a person', async ({ page }) => {
  // Setup: create person
  await apiHelpers.createPerson({ fullName: 'Test Person' });

  // Action: delete through UI
  await pageHelpers.deletePerson('Test Person');

  // Verify: person no longer exists
  await pageHelpers.verifyPersonNotExists('Test Person');
});
```

**When to Use**:
- ✅ Create entity (POST)
- ✅ Read/view entity (GET)
- ✅ Update entity (PUT)
- ✅ Delete entity (DELETE)
- ✅ Required field validation
- ✅ Basic navigation between pages
- ✅ Empty state displays
- ❌ NOT for multiple/bulk operations (use @extended)
- ❌ NOT for edge cases (use @extended)
- ❌ NOT for UI polish features (use @extended)

---

### @extended (~10-15 min, run on merge to dev)

**Purpose**: "Does it handle all cases and edge scenarios?"

**Criteria**:
- Bulk/multiple operations (creating many items at once)
- Complex relationships between entities (person with multiple roles)
- Edge cases and error scenarios (404s, invalid data, special characters)
- UX polish features (form cancellation, reset, refresh)
- Data integrity (referential integrity, cascading deletes)
- Performance scenarios (concurrent operations)
- Optional field handling
- Various input formats (phone numbers, special characters)

**Rule of Thumb**: *"If this fails, some edge cases don't work correctly"*

**Examples**:
```typescript
test('@extended should create multiple people', async ({ page }) => {
  for (const person of testPeople) {
    await pageHelpers.clickAddPerson();
    await pageHelpers.fillPersonForm(person.fullName, person.phone);
    await pageHelpers.submitPersonForm();
  }
  const count = await pageHelpers.getPersonRowCount();
  expect(count).toBe(testPeople.length);
});

test('@extended should handle special characters in person data', async ({ apiHelpers }) => {
  const person = {
    fullName: "O'Brien-Wilson St. James",
    phone: '+1-555-0123'
  };
  const created = await apiHelpers.createPerson(person);
  expect(created.fullName).toContain(person.fullName);
});

test('@extended should handle form cancellation', async ({ page }) => {
  await pageHelpers.clickAddPerson();
  await pageHelpers.fillPersonForm('Test Person', '+1-555-0123');
  await page.click('button:has-text("Cancel")');
  await expect(page.locator('form')).not.toBeVisible();
});

test('@extended should handle 404 for non-existent person', async ({ apiContext }) => {
  const response = await apiContext.get('/api/people/00000000-0000-0000-0000-000000000000');
  expect(response.status()).toBe(404);
});
```

**When to Use**:
- ✅ Bulk operations (create/delete many items)
- ✅ Complex workflows (person with roles, cascading deletes)
- ✅ Error cases (404, 400, invalid IDs)
- ✅ Special input formats (international phone, special chars)
- ✅ UX features (cancel, reset, refresh)
- ✅ Concurrent operations
- ✅ Optional field handling
- ✅ Display formatting and details
- ❌ NOT for basic CRUD (use @critical)
- ❌ NOT for simple availability checks (use @smoke)

---

### @meta (variable time, run on every PR)

**Purpose**: "Are the test infrastructure and configuration correct?"

**Criteria**:
- CI/CD configuration validation
- Test suite coverage verification
- Database cleanup/setup tests
- Test helper infrastructure tests
- Configuration compliance checks
- Test environment validation

**Rule of Thumb**: *"Tests that test the test infrastructure"*

**Examples**:
```typescript
test('@meta should have Playwright webServer configuration', async () => {
  const config = await fs.readFile('playwright.config.ts', 'utf-8');
  expect(config).toContain('webServer:');
  expect(config).not.toContain('globalSetup:');
});

test('@meta @critical Should have proper test categorization', async () => {
  const stats = await calculateTestCategoryStats();
  const untaggedPercentage = (stats.untagged / stats.totalTests) * 100;
  expect(untaggedPercentage).toBeLessThan(20);
});
```

**When to Use**:
- ✅ Configuration validation tests
- ✅ Test suite quality metrics
- ✅ Infrastructure setup/teardown tests
- ✅ Test helper verification
- ❌ NOT for application functionality

---

### @performance (variable time, run on demand or nightly)

**Purpose**: "Does the application meet performance standards?"

**Criteria**:
- Page load time benchmarks
- API response time measurements
- Bulk operation performance
- Concurrent operation handling
- Database operation speed
- Critical path timing

**Rule of Thumb**: *"Measures how fast things are"*

**Examples**:
```typescript
test('@performance @extended Page load performance meets targets', async ({ page, baseURL }) => {
  const startTime = Date.now();
  await page.goto(baseURL);
  await page.waitForSelector('h1');
  const loadTime = Date.now() - startTime;

  expect(loadTime).toBeLessThan(3000); // < 3 seconds
});

test('@performance @critical Smoke test execution time benchmark', async ({ page, apiUrl }) => {
  const startTime = Date.now();

  const operations = [
    () => page.request.get(`${apiUrl}/health`),
    () => page.request.get(`${apiUrl}/api/people`),
    () => page.request.get(`${apiUrl}/api/roles`)
  ];

  for (const op of operations) {
    await op();
  }

  const totalTime = Date.now() - startTime;
  expect(totalTime).toBeLessThan(1000); // < 1 second for all smoke checks
});
```

**When to Use**:
- ✅ Performance benchmarks
- ✅ Load time measurements
- ✅ Response time validations
- ✅ Performance regression detection
- ❌ NOT for functional correctness (use other tags)

---

### @dev (skipped in CI, run manually)

**Purpose**: "Helper and infrastructure tests for local development"

**Criteria**:
- Test helper method validation
- Development utility tests
- Example/documentation tests
- Tests that verify test infrastructure locally

**Rule of Thumb**: *"Tests for developers to verify their test helpers work"*

**Examples**:
```typescript
test.describe.skip('@meta @dev PageHelpers Event-Driven Wait Methods', () => {
  test('should wait for Angular navigation to complete', async ({ page }) => {
    // Validates the helper method works correctly
  });
});
```

**When to Use**:
- ✅ Test helper validation
- ✅ Development utility tests
- ✅ Example tests for documentation
- ❌ NOT for CI/CD execution

---

## Tag Combinations

Tests can have multiple tags to categorize them across dimensions:

- `@meta @smoke` - Infrastructure test that's quick
- `@meta @critical` - Infrastructure test that's essential
- `@performance @smoke` - Quick performance check
- `@performance @critical` - Essential performance benchmark
- `@meta @dev` - Development-only infrastructure test

**Examples**:
```typescript
test('@meta @critical Should have proper test categorization', async () => {
  // Both a meta test (testing test quality) AND critical (must pass)
});

test('@performance @smoke should complete test initialization in under 5 seconds', async () => {
  // Both performance test AND smoke test (quick check)
});
```

---

## Decision Tree

Use this flowchart to categorize a new test:

```
1. Is this testing the test infrastructure itself?
   YES → @meta
   NO → Continue to #2

2. Is this measuring performance/timing?
   YES → @performance (+ @smoke/@critical/@extended based on importance)
   NO → Continue to #3

3. Does it verify the app/API is alive and responding?
   YES → @smoke
   NO → Continue to #4

4. Is it basic CRUD or core functionality users need?
   YES → @critical
   NO → Continue to #5

5. Is it an edge case, bulk operation, or UX polish?
   YES → @extended
   NO → Re-evaluate - you might be missing something
```

---

## Current Distribution Target

Aim for this distribution to balance fast feedback with comprehensive coverage:

| Tag | Target % | Target Count (for ~200 tests) | Run Time |
|-----|----------|-------------------------------|----------|
| @smoke | 35-40% | ~70-80 tests | 2-3 min |
| @critical | 25-30% | ~50-60 tests | 5-10 min |
| @extended | 30-35% | ~60-70 tests | 10-15 min |
| @meta | ~10% | ~20 tests | varies |
| @performance | ~5% | ~10 tests | varies |

**Rationale**:
- **@smoke**: Larger set for fast feedback on every PR
- **@critical**: Moderate set covering core functionality
- **@extended**: Comprehensive coverage for dev/main branches
- **@meta**: Small set for test quality validation
- **@performance**: Small set for performance monitoring

---

## Examples by Feature

### Example: People CRUD

```typescript
// SMOKE - Basic availability
test('@smoke GET /api/people - should return 200', async ({ apiContext }) => {
  const response = await apiContext.get('/api/people');
  expect(response.ok()).toBe(true);
});

// CRITICAL - Core functionality
test('@critical POST /api/people - should create a new person', async ({ apiHelpers }) => {
  const person = { fullName: 'John Doe', phone: '+1-555-0123' };
  const created = await apiHelpers.createPerson(person);
  expect(created.fullName).toBe('John Doe');
});

test('@critical PUT /api/people/{id} - should update existing person', async ({ apiHelpers }) => {
  const person = await apiHelpers.createPerson({ fullName: 'Jane Doe' });
  const updated = await apiHelpers.updatePerson(person.id, { fullName: 'Jane Smith' });
  expect(updated.fullName).toBe('Jane Smith');
});

test('@critical DELETE /api/people/{id} - should delete existing person', async ({ apiHelpers }) => {
  const person = await apiHelpers.createPerson({ fullName: 'Test Person' });
  await apiHelpers.deletePerson(person.id);
  const people = await apiHelpers.getPeople();
  expect(people.find(p => p.id === person.id)).toBeUndefined();
});

// EXTENDED - Edge cases
test('@extended POST /api/people - should handle special characters', async ({ apiHelpers }) => {
  const person = { fullName: "O'Brien-Wilson", phone: '+1-555-0123' };
  const created = await apiHelpers.createPerson(person);
  expect(created.fullName).toContain("O'Brien");
});

test('@extended should create multiple people', async ({ apiHelpers }) => {
  const people = [
    { fullName: 'Person 1' },
    { fullName: 'Person 2' },
    { fullName: 'Person 3' }
  ];
  for (const person of people) {
    await apiHelpers.createPerson(person);
  }
  const allPeople = await apiHelpers.getPeople();
  expect(allPeople.length).toBeGreaterThanOrEqual(3);
});

test('@extended GET /api/people/{id} - should return 404 for non-existent', async ({ apiContext }) => {
  const response = await apiContext.get('/api/people/00000000-0000-0000-0000-000000000000');
  expect(response.status()).toBe(404);
});
```

---

## Anti-Patterns to Avoid

### ❌ Don't Over-Categorize
```typescript
// BAD - Too many tags
test('@smoke @critical @extended should create person', async () => {
  // A test should be one primary category
});
```

### ❌ Don't Under-Categorize Core Functionality
```typescript
// BAD - Core CRUD should be @critical, not @extended
test('@extended should create a person', async () => {
  // Creating a person is core functionality!
});
```

### ❌ Don't Put Long-Running Tests in @smoke
```typescript
// BAD - This takes too long for smoke tests
test('@smoke should create 100 people and verify all', async () => {
  // Bulk operations belong in @extended
});
```

### ❌ Don't Mix Test Infrastructure with Application Tests
```typescript
// BAD - This is testing playwright config, not the app
test('@critical should have webServer configured', async () => {
  // This should be @meta, not @critical
});
```

---

## Maintenance

- **Review quarterly**: Check if categorization still matches application priorities
- **Update guide**: When adding new features, update examples in this guide
- **Monitor run times**: If categories drift from target times, rebalance
- **Track coverage**: Ensure all critical paths have @critical tests

---

## Questions?

If you're unsure how to categorize a test:
1. Ask: "What breaks if this test fails?"
   - Everything? → @smoke
   - Core user task? → @critical
   - Edge case? → @extended
   - Test infrastructure? → @meta

2. Consult this guide's decision tree

3. Ask the team in code review

**Last Updated**: 2025-10-08
