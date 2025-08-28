# E2E Test Architecture Implementation Roadmap

## Phase 1: In-Memory Database (Immediate Fix)
**Timeline: 1-2 days**
**Impact: Solves 80% of issues**

1. Add in-memory database support to API:
```csharp
// Program.cs or Startup.cs
if (Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true")
{
    services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options.UseSqlite(connection);
        // Store connection in DI to keep it alive
        services.AddSingleton(connection);
    });
}
```

2. Update worker setup to use in-memory:
```typescript
// tests/setup/worker-setup.ts
const apiEnv = {
  ...process.env,
  USE_IN_MEMORY_DB: 'true', // Enable in-memory for tests
  ASPNETCORE_URLS: `http://localhost:${this.apiPort}`,
}
```

3. Remove complex cleanup logic - database resets automatically!

## Phase 2: Test Organization (1 day)
**Timeline: 1 day**
**Impact: Reduces server count by 60%**

1. Separate API and UI tests:
```
tests/
  api/          # No browser needed
  ui/           # Needs Angular + browser
  integration/  # Full stack tests
```

2. Create targeted configs:
- `playwright.config.api.ts` - No browsers, 1 shared server
- `playwright.config.ui.ts` - Chromium only, 2 workers max
- `playwright.config.full.ts` - All browsers (CI only)

## Phase 3: Smart Worker Pool (2-3 days)
**Timeline: 2-3 days**
**Impact: Optimizes resource usage**

1. Implement server pooling:
```typescript
class ServerPool {
  private static readonly MAX_SERVERS = 2;
  private static servers = new Map<number, ServerInstance>();
  
  static async getServer(workerIndex: number) {
    const poolIndex = workerIndex % this.MAX_SERVERS;
    
    if (!this.servers.has(poolIndex)) {
      const server = await this.startServer(poolIndex);
      this.servers.set(poolIndex, server);
    }
    
    return this.servers.get(poolIndex);
  }
}
```

2. Update fixtures to use pool:
```typescript
workerServerManager: [async ({ }, use, workerInfo) => {
  const server = await ServerPool.getServer(workerInfo.workerIndex);
  await use(server);
}, { scope: 'worker' }]
```

## Phase 4: CI/CD Optimization (1 day)
**Timeline: 1 day**
**Impact: Faster CI, better parallelization**

1. Update GitHub Actions:
```yaml
strategy:
  matrix:
    test-suite: [api, ui-chromium, ui-firefox, ui-webkit]
    
steps:
  - name: Run tests
    run: |
      if [[ "${{ matrix.test-suite }}" == "api" ]]; then
        npm run test:api
      elif [[ "${{ matrix.test-suite }}" == "ui-chromium" ]]; then
        npm test -- --project=chromium
      # etc...
```

2. Add test result aggregation

## Phase 5: Monitoring & Optimization (Ongoing)

1. Add server health monitoring
2. Implement test timing analysis
3. Optimize slow tests
4. Add flaky test detection

## Quick Wins (Do Today!)

1. **Switch to in-memory database for tests** - Biggest impact
2. **Reduce workers to 1-2** - Immediate resource savings
3. **Run single browser locally** - Faster development
4. **Separate API tests** - They don't need browsers

## Configuration Examples

### Local Development (Fast)
```bash
npm run test:api        # API only, no browser
npm run test:ui         # UI with Chromium only
```

### CI Pipeline (Thorough)
```bash
npm run test:ci         # All browsers, all tests
```

### Debugging
```bash
npm run test:single     # Single worker, easier debugging
```

## Success Metrics

- [ ] Test execution time < 5 minutes
- [ ] Zero database cleanup failures  
- [ ] Max 4 server processes running
- [ ] 100% test pass rate
- [ ] No flaky tests

## Rollback Plan

If issues arise, you can:
1. Revert to file-based SQLite (current approach)
2. Use single worker mode temporarily
3. Run tests sequentially in CI

The beauty of this approach is it's incremental - you can implement Phase 1 today and see immediate benefits!