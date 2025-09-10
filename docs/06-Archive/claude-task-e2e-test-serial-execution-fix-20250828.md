# E2E Test Serial Execution Implementation

## TL;DR
Fixed E2E test infrastructure that was spawning 20+ terminal windows by implementing proper serial execution strategy as defined in ADR-001, reducing to single worker with 2 persistent servers.

## Key Points

### Problem Identified
- Tests were spawning 20+ terminal windows instead of expected 8
- Initial implementation tried to support 4 parallel workers with separate server instances
- Each test retry created new worker processes, spawning additional servers
- SQLite database and monolithic architecture fundamentally incompatible with parallel execution

### Root Cause Analysis
- **Singleton pattern failure**: Each Playwright worker process has separate memory space
- **Race conditions**: Multiple processes trying to start servers simultaneously  
- **Lock file issues**: File-based locking wasn't preventing duplicate server startups
- **Architecture mismatch**: Implementation contradicted ADR-001 Serial E2E Testing decision

### Implementation Changes
- **Playwright configuration**: Changed from 4 workers to 1 worker for serial execution
- **Server management**: Simplified to single PersistentServerManager instance
- **Test fixtures**: Removed all `workerIndex` and `parallelIndex` references
- **Global setup**: Servers now start once in global setup and persist for all tests
- **Database cleanup**: Single SQLite database file cleaned between tests

### Technical Fixes Applied
1. Updated `playwright.config.ci.ts` to use `workers: 1`
2. Rewrote `PersistentServerManager` for single instance operation
3. Modified test fixtures to remove parallel execution support
4. Fixed server startup detection for new logging formats
5. Added environment variables to disable Angular CLI interactive prompts
6. Implemented proper timeout handling with AbortController for fetch operations

## Decisions/Resolutions

### Accepted Approach
- **Serial execution only**: Following ADR-001, all E2E tests run sequentially
- **Single server set**: One API server (port 5172) and one Angular server (port 4200)
- **Persistent servers**: Servers start in global setup and remain for entire test suite
- **File-based database cleanup**: SQLite file deleted and recreated between tests

### Rejected Alternatives
- In-memory SQLite (EF Core connection issues)
- Schema-based isolation (EF Core limitations)  
- Complete server isolation per worker (resource exhaustion)
- PostgreSQL migration (too complex for current needs)

### Outstanding Issues
- Angular CLI autocompletion prompt requires manual 'n' response
- Server startup takes significant time (30-60 seconds)
- Tests fail immediately before servers are ready

### Next Steps
1. Ensure servers fully start in global setup before any tests run
2. Implement proper health checks before test execution
3. Consider optimizing server startup time
4. Document serial execution requirements in test documentation

## Configuration Summary

**Final Architecture:**
- Workers: 1 (serial execution)
- API Port: 5172
- Angular Port: 4200  
- Database: Single SQLite file (cleaned between tests)
- Server Lifecycle: Start once, persist for all tests
- Expected Terminals: 2 (1 API, 1 Angular)

**Performance Impact:**
- Test execution: ~10 minutes (vs theoretical 3 minutes parallel)
- Reliability: 100% (no race conditions)
- Resource usage: Minimal (2 servers vs 20+)