# Angular E2E Tests and Server Management Fix - Discussion Summary
**Date:** August 28, 2025

## Problem Statement

### Initial Issue
The GitHub Actions PR validation workflow was failing with Angular server startup timeout during E2E tests. The workflow was stuck waiting for Angular to be ready on port 4200.

**Root Cause:** The Angular `npm start` command referenced a non-existent `proxy.conf.json` file, causing the server to fail to start in CI environments.

## Solutions Implemented

### 1. Fixed Angular Startup Issue

#### Changes Made:
- **Created `proxy.conf.json`** for local development with proper API proxy configuration
- **Added CI-specific start script** in `package.json`:
  ```json
  "start:ci": "ng serve --host 0.0.0.0"
  ```
- **Updated PR validation workflow** to use `npm run start:ci` instead of `npm start`

#### Key Files Modified:
- `src/Angular/package.json` - Added start:ci script
- `src/Angular/proxy.conf.json` - Created proxy configuration
- `.github/workflows/pr-validation.yml` - Updated to use CI script

### 2. Enhanced Workflow Error Handling

Added comprehensive error handling and debugging capabilities to the PR validation workflow:

- **Server startup logging** - Captures output to `api.log` and `angular.log`
- **Progressive health checks** - Shows attempt counters and intermediate log checks
- **Improved error messages** - Clear status indicators with emojis
- **Log artifact upload** - Preserves server logs on failure for debugging
- **Final health verification** - Ensures both servers are responsive before running tests

### 3. E2E Test Terminal Window Management

#### Issue Discovered:
User was seeing multiple terminal windows spawning during E2E test execution, even though servers should be reused.

#### Solution:
Added `windowsHide: true` option to spawn commands in test setup files:
- `persistent-server-manager.ts`
- `worker-setup.ts`
- `server-pool.ts`

*Note: User later changed this to `windowsHide: false` to keep terminals visible for debugging*

## E2E Test Architecture Analysis

### Server Management Strategy

The E2E tests use a **Persistent Server Manager** approach:

1. **Server Reuse Pattern**
   - Each parallel worker gets dedicated servers on different ports:
     - Worker 0: API 5172, Angular 4200
     - Worker 1: API 5173, Angular 4210
     - Worker 2: API 5174, Angular 4220
   - Servers stay alive for 5 minutes and are reused between tests
   - Database is reset via API endpoint between tests (not server restart)

2. **Why This Design**
   - **Performance:** Server startup is expensive (5-10 seconds each)
   - **Isolation:** Each worker has completely isolated servers and databases
   - **Efficiency:** Only 3 server pairs for 100+ tests instead of 100+ pairs

### Bug Found: Premature Server Termination

#### The Problem:
A race condition in `PersistentServerManager` was causing servers to be killed and restarted unnecessarily:

1. Test 2 checks if servers from Test 1 are healthy
2. If servers are slow to respond, health check fails
3. Code attempts to "start new servers"
4. Finds ports in use (by still-running servers!)
5. **Kills those servers** without proper verification
6. Starts new servers → New terminal windows appear

#### The Fix:
Implemented two key improvements:

1. **Better retry logic:** 
   - Tries 3 times with 2-second delays before giving up on existing servers
   - Gives slow servers more time to respond

2. **Smarter port conflict resolution:**
   - Double-checks if processes on ports are actually responding
   - Only kills truly non-responsive processes
   - Won't kill a server that's just slow

## Key Takeaways

1. **CI/CD environments need different configurations** - The proxy that works for local development may not be needed in CI where services run separately

2. **Server reuse in tests requires careful management** - Race conditions can cause unnecessary server restarts, defeating the purpose of reuse

3. **Comprehensive logging is essential** - The enhanced workflow logging makes debugging CI failures much easier

4. **Test isolation strategies involve trade-offs**:
   - Full restart per test: Complete isolation but very slow
   - Server reuse with DB reset: Fast with good-enough isolation
   - The current approach balances speed and isolation well

## Files Modified Summary

### Workflow & Configuration
- `.github/workflows/pr-validation.yml` - Enhanced error handling and logging
- `src/Angular/package.json` - Added CI-specific start script
- `src/Angular/proxy.conf.json` - Created proxy configuration

### Test Infrastructure
- `test/Tests.E2E.NG/tests/setup/persistent-server-manager.ts` - Fixed race condition
- `test/Tests.E2E.NG/tests/setup/worker-setup.ts` - Added windowsHide option
- `test/Tests.E2E.NG/tests/setup/server-pool.ts` - Added windowsHide option

### Documentation
- `tasks.md` - Tracked implementation progress

## Result

The Angular startup timeout issue in CI/CD is now resolved, with improved error handling and debugging capabilities. The E2E test server management has been optimized to properly reuse servers, reducing unnecessary terminal windows and improving test execution efficiency.