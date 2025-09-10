# E2E Test Excessive Terminal Spawning Fix
Date: 2025-08-28

## Problem
When running `npm test:ci`, the test suite was spawning more than 20 terminal windows instead of the expected 8 (4 parallel workers × 2 servers each).

## Investigation

### Initial Analysis
1. Examined the test configuration:
   - `playwright.config.ci.ts` configured with 4 parallel workers
   - Each worker needs API server (port 5172+index) and Angular server (port 4200+index*10)
   - Expected: 8 total terminals (4 workers × 2 servers)

2. Identified the root cause in `tests/setup/persistent-server-manager.ts`:
   - Multiple tests were creating their own `PersistentServerManager` instances
   - Race condition: Multiple tests in same worker tried starting servers simultaneously
   - Lock file wasn't preventing duplicate startups within same millisecond
   - Server processes configured with `detached: true` and `windowsHide: false` (showing terminals)

## Solution Implemented

### 1. Singleton Pattern
Added singleton management to `PersistentServerManager`:
```typescript
private static instances: Map<number, PersistentServerManager> = new Map();
private static serverStartPromises: Map<number, Promise<any>> = new Map();

static getInstance(parallelIndex: number): PersistentServerManager {
  if (!this.instances.has(parallelIndex)) {
    this.instances.set(parallelIndex, new PersistentServerManager(parallelIndex));
  }
  return this.instances.get(parallelIndex)!;
}
```

### 2. Synchronization
Added promise-based synchronization to prevent duplicate server startups:
```typescript
async ensureServers(): Promise<{ apiUrl: string; angularUrl: string; database: string }> {
  // Check if another test is already starting servers
  const existingPromise = PersistentServerManager.serverStartPromises.get(this.parallelIndex);
  if (existingPromise) {
    await existingPromise;
    if (this.serverInfo) {
      return this.serverInfo;
    }
  }
  // Continue with server startup...
}
```

### 3. Result Caching
Added caching to avoid redundant server checks:
```typescript
private serverInfo: { apiUrl: string; angularUrl: string; database: string } | null = null;

// Return cached info if available
if (this.serverInfo) {
  return this.serverInfo;
}
```

### 4. Updated Test Fixtures
Modified both `test-fixture.ts` and `api-only-fixture.ts` to use the singleton:
```typescript
const manager = PersistentServerManager.getInstance(parallelIndex);
```

## Key Configuration Details

### Port Assignments
- API ports: 5172, 5173, 5174, 5175 (base + parallel index)
- Angular ports: 4200, 4210, 4220, 4230 (base + parallel index × 10)

### Terminal Window Visibility
- Kept `windowsHide: false` to maintain visibility of server processes
- User specifically requested to see the terminals (not hidden)

## Result
The fix ensures exactly 8 terminal windows open:
- 4 API server terminals
- 4 Angular server terminals
- No duplicate servers within same parallel index
- Servers persist and are reused across all tests in the suite

## Technical Notes
- The singleton pattern prevents multiple manager instances per parallel index
- Promise-based synchronization handles concurrent test execution
- Lock file still used for cross-process coordination but supplemented with in-memory state
- Server processes remain detached to persist beyond individual test execution