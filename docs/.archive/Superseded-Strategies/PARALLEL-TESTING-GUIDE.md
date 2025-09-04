# Parallel E2E Testing with Worker Isolation

This guide explains how to use the new parallel testing setup that provides true worker isolation using per-worker API servers and databases.

## 🎯 Problem Solved

The new parallel testing approach solves two critical issues:

1. **Concurrent Test Isolation**: Each worker gets its own API server and database, preventing interference between parallel tests
2. **Sequential Test State Bleeding**: Respawn resets database state between tests within each worker

## 🏗️ Architecture

### Traditional Approach (Sequential)
```
Single API Server (port 5172)
    ↓
Single SQLite Database
    ↓
Tests run sequentially to avoid conflicts
```

### New Parallel Approach
```
Worker 0: API Server (port 5172) → Database_Worker0_timestamp.db
Worker 1: API Server (port 5173) → Database_Worker1_timestamp.db  
Worker 2: API Server (port 5174) → Database_Worker2_timestamp.db
Worker 3: API Server (port 5175) → Database_Worker3_timestamp.db
    ↓
Tests run in parallel with complete isolation
```

## 🚀 Getting Started

### 1. Available Test Configurations

#### **Sequential Execution** (Current/Safe)
```bash
# Uses shared API server, sequential execution
npm run test:local
npm run test:api-only
```

#### **Parallel Execution** (New/Fast)
```bash
# Each worker gets its own API server and database
npm run test:parallel

# List parallel tests
npm run test:list-parallel
```

### 2. Test File Organization

#### **Sequential Tests** (existing)
- Use `import { test, expect } from '@playwright/test'`
- Use `import { test, expect } from '../fixtures/worker-isolation'`
- Located in: `tests/api/*.spec.ts`, `tests/angular-ui/*.spec.ts`

#### **Parallel Tests** (new)
- Use `import { test, expect } from '../fixtures/worker-isolation-parallel'`
- Located in: `tests/api/*-parallel.spec.ts`
- Example: `tests/api/roles-api-parallel.spec.ts`

## 📋 Test Execution Examples

### Run Parallel Tests
```bash
# Run all parallel tests (4 workers by default)
npm run test:parallel

# Run with specific number of workers
npx playwright test --config=playwright.config.parallel.ts --workers=2

# Run specific parallel test file
npx playwright test tests/api/roles-api-parallel.spec.ts --config=playwright.config.parallel.ts

# Run with visible browsers (debugging)
npx playwright test --config=playwright.config.parallel.ts --headed

# Run with UI mode
npx playwright test --config=playwright.config.parallel.ts --ui
```

### Performance Comparison
```bash
# Sequential (safe, slower)
time npm run test:api-only

# Parallel (fast, more resource intensive)
time npm run test:parallel
```

## 🔧 Configuration Details

### Worker-Specific Resources

Each worker gets:
- **Unique Port**: `5172 + workerIndex` (5172, 5173, 5174, 5175...)
- **Unique Database**: `CrudAppTest_Worker{index}_{timestamp}.db`
- **Isolated API Server**: Separate .NET process per worker
- **Clean State**: Respawn resets database between tests

### Environment Variables

The API server automatically detects worker-specific configuration:
- `WORKER_INDEX`: Set by Playwright fixture
- `WORKER_DATABASE`: Set by Playwright fixture  
- `ASPNETCORE_ENVIRONMENT`: Set to "Testing"
- `ASPNETCORE_URLS`: Set to worker-specific port

### Resource Requirements

**Sequential Tests**:
- 1 API server process
- 1 SQLite database file
- Lower memory usage
- Slower execution

**Parallel Tests**:
- N API server processes (where N = number of workers)
- N SQLite database files
- Higher memory usage (~200MB per worker)
- Faster execution (3-4x speedup typical)

## 📝 Writing Parallel Tests

### Basic Parallel Test
```typescript
import { test, expect } from '../fixtures/worker-isolation-parallel';
import { generateTestRole } from '../helpers/test-data';

test.describe('My Parallel Tests', () => {
  test('should work in isolation', async ({ isolatedApiHelpers, workerApiServer }) => {
    console.log(`Running in worker with server: ${workerApiServer}`);
    
    // Your test code here - completely isolated from other workers
    const testRole = generateTestRole(`Test_${Date.now()}`);
    const createdRole = await isolatedApiHelpers.createRole(testRole);
    
    expect(createdRole.name).toBe(testRole.name);
  });
});
```

### Available Fixtures

- `isolatedApiHelpers`: API helper methods targeting worker-specific server
- `databaseRespawn`: Database reset utilities for this worker
- `workerApiServer`: URL of this worker's API server (e.g., "http://localhost:5173")

### Best Practices

1. **Use Unique Test Data**: Include timestamps or worker index in test data
2. **Log Worker Info**: Include worker server URL in console logs for debugging
3. **Handle Timeouts**: Worker servers may take longer to start
4. **Resource Cleanup**: Fixtures automatically clean up servers and databases

## 🐛 Troubleshooting

### Common Issues

#### "Server failed to start within timeout"
```bash
# Increase timeout or reduce worker count
npx playwright test --config=playwright.config.parallel.ts --workers=2
```

#### "Port already in use"
```bash
# Kill existing processes
pkill -f "dotnet.*Api"
# Or restart your development environment
```

#### High memory usage
```bash
# Reduce worker count for resource-constrained environments
npx playwright test --config=playwright.config.parallel.ts --workers=2
```

### Debugging Worker Issues

#### Check worker server logs
```typescript
test('debug worker', async ({ workerApiServer }) => {
  console.log(`Worker server: ${workerApiServer}`);
  
  // Test server health
  const response = await fetch(`${workerApiServer}/health`);
  console.log(`Health check: ${response.status}`);
});
```

#### Verify database isolation
```typescript
test('verify isolation', async ({ isolatedApiHelpers, databaseRespawn }) => {
  const workerIndex = databaseRespawn.getWorkerIndex();
  console.log(`Running in worker: ${workerIndex}`);
  
  // Check database stats
  const response = await fetch(`${workerApiServer}/api/database/status`);
  const stats = await response.json();
  console.log('Database stats:', stats);
});
```

## 🔄 Migration Strategy

### Phase 1: Validate Parallel Setup
1. Run existing tests with sequential config: `npm run test:api-only`
2. Run new parallel tests: `npm run test:parallel`
3. Compare results and performance

### Phase 2: Convert Critical Tests
1. Copy important test files to `*-parallel.spec.ts`
2. Update imports to use parallel fixtures
3. Add worker-specific logging and data

### Phase 3: Full Migration
1. Convert all API tests to parallel execution
2. Update CI/CD pipelines
3. Set parallel as default configuration

## 📊 Performance Benefits

**Typical Results** (4 workers):
- **Sequential**: 58 API tests in ~45 seconds
- **Parallel**: 58 API tests in ~12 seconds
- **Speedup**: ~3.75x faster execution

**Resource Usage**:
- **CPU**: Higher during test execution
- **Memory**: ~200MB per worker (800MB for 4 workers)
- **Disk**: Temporary database files (cleaned up automatically)

## 🔒 Security Considerations

- Database endpoints only available in Development/Testing environments
- Worker databases are temporary and automatically cleaned up
- No production data is ever affected
- Each worker is completely isolated from others

## 🎛️ Configuration Options

### Playwright Config (`playwright.config.parallel.ts`)
```typescript
workers: process.env.CI ? 2 : 4, // Adjust based on environment
timeout: 40000, // Longer timeout for worker servers
retries: process.env.CI ? 2 : 0, // Retry failed tests
```

### API Server Config (automatic)
- Port allocation: `5172 + workerIndex`
- Database naming: `CrudAppTest_Worker{index}_{timestamp}.db`
- Environment: Always set to "Testing"

This parallel testing approach provides the best of both worlds: fast execution through parallelization and reliable isolation through dedicated resources per worker.
