# Serial Test Optimization Plan

## Current State Assessment

### The Numbers
- **Test Files**: 9 spec files
- **Test Cases**: 107 unique tests
- **Total Executions**: 321 (107 tests × 3 browsers)
- **Current Runtime**: ~20-30 minutes (estimated)
- **Server Startup**: 30-60 seconds per test file
- **Database Issues**: Cleanup failures, contamination

### Current Problems
1. **Every test file spawns new servers** (9 files × 2 servers = 18 server startups)
2. **Database cleanup fails randomly** (SQLite locks, EF Core issues)
3. **Running same tests on 3 browsers** (3x execution time)
4. **No test categorization** (all tests run always)
5. **Slow feedback loop** (20+ minutes to know if anything broke)

## Future State Vision

### Target Metrics
- **Smoke Tests**: < 2 minutes (critical path only)
- **Full Suite**: < 10 minutes (all tests, single browser)
- **Cross-Browser**: < 15 minutes (critical tests only, 3 browsers)
- **Server Startups**: 1 (not 18)
- **Database Cleanup**: 100% reliable

### Architecture
```
┌─────────────────────────────────────────┐
│         SINGLE TEST RUN LIFECYCLE        │
├─────────────────────────────────────────┤
│ 1. Start servers once (30s)              │
│ 2. Run all tests serially                │
│ 3. Reset DB between tests (not files)    │
│ 4. Stop servers once                     │
└─────────────────────────────────────────┘
```

## Migration Plan: From Current to Future

### Phase 1: Fix Database Cleanup (Day 1)
**Goal**: Make serial execution reliable

#### Step 1.1: Replace Complex Cleanup with Simple Reset
```csharp
// DatabaseTestService.cs
public async Task ResetDatabase()
{
    // Close all connections first
    await _context.Database.CloseConnectionAsync();
    
    // Delete and recreate file (most reliable for SQLite)
    var connString = _context.Database.GetConnectionString();
    var dbPath = new SqliteConnectionStringBuilder(connString).DataSource;
    
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
    }
    
    // Recreate with fresh schema
    await _context.Database.EnsureCreatedAsync();
    
    // Seed minimal data if needed
    await SeedRequiredData();
}
```

#### Step 1.2: Add Retry Logic for SQLite Locks
```csharp
public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, int maxAttempts = 3)
{
    for (int i = 0; i < maxAttempts; i++)
    {
        try
        {
            return await operation();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
        {
            if (i == maxAttempts - 1) throw;
            await Task.Delay(100 * (i + 1)); // Exponential backoff
        }
    }
}
```

### Phase 2: Optimize Test Organization (Day 2)
**Goal**: Categorize tests by priority and type

#### Step 2.1: Tag Tests by Category
```typescript
// tests/critical/auth.spec.ts
test.describe('@smoke @critical Authentication', () => {
  test('user can login', async ({ page }) => {
    // Critical path test
  });
});

// tests/extended/reports.spec.ts  
test.describe('@extended Reports', () => {
  test('generate quarterly report', async ({ page }) => {
    // Nice to have, not critical
  });
});
```

#### Step 2.2: Create Test Suites
```typescript
// playwright.config.smoke.ts - 2 minutes
export default defineConfig({
  testDir: './tests',
  grep: /@smoke/,  // Only run smoke tests
  projects: [{ name: 'chromium' }], // Single browser
  timeout: 15000, // Shorter timeout
  workers: 1,
  retries: 0, // No retries for speed
});

// playwright.config.full.ts - 10 minutes
export default defineConfig({
  testDir: './tests',
  projects: [{ name: 'chromium' }], // Still single browser
  timeout: 30000,
  workers: 1,
  retries: 1,
});

// playwright.config.cross-browser.ts - 15 minutes
export default defineConfig({
  testDir: './tests',
  grep: /@critical/, // Only critical tests
  projects: [
    { name: 'chromium' },
    { name: 'firefox' },
    { name: 'webkit' }
  ],
  workers: 1, // Serial execution
});
```

### Phase 3: Server Lifecycle Optimization (Day 3)
**Goal**: Start servers once, not 18 times

#### Step 3.1: Global Setup with Health Checks
```typescript
// tests/setup/global-setup.ts
import { spawn } from 'child_process';
import waitOn from 'wait-on';

let apiProcess: any;
let angularProcess: any;

async function globalSetup() {
  console.log('🚀 Starting test servers (once for all tests)...');
  
  // Start API
  apiProcess = spawn('dotnet', ['run', '--no-build'], {
    cwd: '../../src/Api',
    env: {
      ...process.env,
      ASPNETCORE_URLS: 'http://localhost:5555',
      ASPNETCORE_ENVIRONMENT: 'Testing',
    },
    stdio: 'pipe'
  });
  
  // Build Angular first (if needed)
  if (!process.env.SKIP_ANGULAR_BUILD) {
    console.log('📦 Building Angular...');
    execSync('npm run build', { cwd: '../../src/Angular' });
  }
  
  // Start Angular
  angularProcess = spawn('npm', ['run', 'start:prebuilt'], {
    cwd: '../../src/Angular',
    env: {
      ...process.env,
      PORT: '4444'
    },
    stdio: 'pipe'
  });
  
  // Wait for both
  await waitOn({
    resources: [
      'http://localhost:5555/health',
      'http://localhost:4444'
    ],
    timeout: 60000,
    interval: 1000
  });
  
  console.log('✅ Servers ready');
  
  // Store PIDs for cleanup
  process.env.API_PID = apiProcess.pid;
  process.env.ANGULAR_PID = angularProcess.pid;
}

async function globalTeardown() {
  console.log('🛑 Stopping test servers...');
  
  if (process.env.API_PID) {
    process.kill(parseInt(process.env.API_PID));
  }
  
  if (process.env.ANGULAR_PID) {
    process.kill(parseInt(process.env.ANGULAR_PID));
  }
}

export default globalSetup;
```

#### Step 3.2: Single Test Context
```typescript
// tests/fixtures/serial-fixture.ts
export const test = base.extend({
  // Reset database before EACH test (not test file)
  autoCleanup: [async ({ request }, use) => {
    // Pre-test cleanup
    await request.post('http://localhost:5555/api/test/reset-database');
    
    await use();
    
    // Post-test cleanup (optional)
  }, { auto: true }],
  
  // Reuse single page context for speed
  context: async ({ browser }, use) => {
    const context = await browser.newContext({
      baseURL: 'http://localhost:4444',
      // Preset auth cookies if needed
    });
    await use(context);
    await context.close();
  },
});
```

### Phase 4: Test Speed Optimizations (Day 4)
**Goal**: Make individual tests faster

#### Step 4.1: Batch API Operations
```typescript
// Before: Multiple API calls
await apiHelpers.createUser({ name: 'Alice' });
await apiHelpers.createUser({ name: 'Bob' });
await apiHelpers.createRole({ name: 'Admin' });

// After: Batch operation
await apiHelpers.seedTestData({
  users: [{ name: 'Alice' }, { name: 'Bob' }],
  roles: [{ name: 'Admin' }]
});
```

#### Step 4.2: Smart Waits Instead of Fixed Delays
```typescript
// Before
await page.click('#submit');
await page.waitForTimeout(2000); // Always waits 2s

// After
await page.click('#submit');
await page.waitForResponse(resp => 
  resp.url().includes('/api/save') && resp.status() === 200,
  { timeout: 5000 }
); // Waits only as long as needed
```

#### Step 4.3: Reduce Browser Overhead
```typescript
// playwright.config.ts
use: {
  // Disable things we don't need
  screenshot: 'only-on-failure',
  video: 'off', // Don't record video
  trace: 'off', // Don't collect traces
  
  // Faster navigation
  navigationTimeout: 10000, // Reduced from 45000
  actionTimeout: 5000, // Reduced from 15000
  
  // Skip animations
  launchOptions: {
    args: ['--disable-web-security', '--disable-features=IsolateOrigins,site-per-process']
  }
}
```

### Phase 5: CI/CD Pipeline (Day 5)
**Goal**: Fast feedback, comprehensive coverage

#### Step 5.1: Staged Pipeline
```yaml
# .github/workflows/test.yml
jobs:
  # Stage 1: Quick smoke tests (2 min)
  smoke-tests:
    runs-on: ubuntu-latest
    steps:
      - run: npm run test:smoke
      - name: Fail fast
        if: failure()
        run: exit 1

  # Stage 2: Full suite, single browser (10 min)
  full-tests:
    needs: smoke-tests
    runs-on: ubuntu-latest
    steps:
      - run: npm run test:full

  # Stage 3: Cross-browser, critical only (15 min)
  cross-browser:
    needs: smoke-tests
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' # Only on main branch
    steps:
      - run: npm run test:cross-browser
```

#### Step 5.2: Parallel by Test Type (Not Workers)
```yaml
jobs:
  test-matrix:
    strategy:
      matrix:
        suite: [api, ui-critical, ui-extended]
    
    steps:
      - run: npm run test:${{ matrix.suite }}
```

## Implementation Timeline

### Week 1: Foundation
- **Monday**: Fix database cleanup (Phase 1)
- **Tuesday**: Implement test categorization (Phase 2)
- **Wednesday**: Global server setup (Phase 3.1)
- **Thursday**: Test fixture optimization (Phase 3.2)
- **Friday**: Test and stabilize

### Week 2: Optimization
- **Monday**: Speed optimizations (Phase 4)
- **Tuesday**: CI pipeline setup (Phase 5)
- **Wednesday-Thursday**: Migrate existing tests
- **Friday**: Performance testing

## Expected Outcomes

### Performance Gains
| Metric | Current | Target | Improvement |
|--------|---------|--------|-------------|
| Smoke Tests | N/A | 2 min | - |
| Full Suite (1 browser) | 20-30 min | 10 min | 50-67% faster |
| Cross-browser | 60-90 min | 15 min | 75-83% faster |
| Server Startups | 18 | 1 | 94% reduction |
| Flaky Tests | ~30% | <5% | 83% reduction |

### Developer Experience
- **Local Testing**: `npm test:smoke` → 2 min feedback
- **Pre-commit**: `npm test:changed` → Test only modified code
- **CI**: Fail fast with smoke tests
- **Debugging**: Reliable reproduction, clear logs

## Quick Wins (Do Today)

1. **Switch to Single Worker**
```json
// package.json
"test": "playwright test --workers=1"
```

2. **Disable Retries**
```typescript
// playwright.config.ts
retries: 0, // Stop masking flaky tests
```

3. **Run Single Browser Locally**
```typescript
projects: [
  { name: 'chromium' } // Comment out firefox, webkit
]
```

4. **Add Smoke Test Script**
```json
"test:smoke": "playwright test --grep @smoke --workers=1"
```

These changes alone should cut runtime by 50% immediately.

## Success Metrics

- [ ] Zero flaky tests for 5 consecutive runs
- [ ] Smoke tests under 2 minutes
- [ ] Full suite under 10 minutes
- [ ] Developer satisfaction improved
- [ ] CI costs reduced by 60%

## The Bottom Line

**Serial testing doesn't have to be slow.** By:
1. Starting servers once (not 18 times)
2. Fixing database cleanup properly
3. Categorizing tests by importance
4. Optimizing individual test speed
5. Running only necessary tests

You can achieve **10-minute full test runs** with **100% reliability**.

This isn't fancy, but it works and ships this week.