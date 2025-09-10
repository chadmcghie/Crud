# Polly Resilience Pattern Implementation

## Overview
This document describes the improvements made to the E2E test infrastructure using Polly-inspired resilience patterns to eliminate terminal spam and improve test reliability.

## Key Improvements

### 1. Backend Polly Integration
- **Added Polly NuGet packages** to Api and Infrastructure projects
- **HTTP Client Resilience**: Retry policies with exponential backoff for HTTP requests
- **Database Resilience**: Retry and circuit breaker policies for database operations
- **Test-specific policies**: Faster retry patterns optimized for testing

### 2. Frontend Test Resilience (TypeScript)
- **ApiHelpers with Retry Logic**: Implements exponential backoff with jitter
- **Circuit Breaker Pattern**: Prevents cascading failures during test execution
- **Intelligent Retry Conditions**: Retries on transient errors, timeouts, and race conditions

### 3. Improved Server Management
- **Server Reuse**: Servers are kept alive and reused across test runs
- **No Terminal Spam**: Uses detached processes with `windowsHide` flag
- **State Persistence**: Server state saved to temp directory for cross-run reuse
- **Health Monitoring**: Regular health checks ensure server availability
- **Smart Port Management**: Automatic port conflict resolution

## Files Created/Modified

### Backend (C#)
- `src/Infrastructure/Resilience/PollyPolicies.cs` - Core Polly policies
- `src/Infrastructure/Resilience/ResilientDbContext.cs` - Database resilience wrapper
- `src/Api/Program.cs` - Integrated Polly into HTTP clients
- `src/Api/Api.csproj` - Added Microsoft.Extensions.Http.Polly
- `src/Infrastructure/Infrastructure.csproj` - Added Polly packages

### Frontend Tests (TypeScript)
- `test/Tests.E2E.NG/tests/setup/improved-server-manager.ts` - Server lifecycle management
- `test/Tests.E2E.NG/tests/setup/improved-test-fixture.ts` - Enhanced test fixtures
- `test/Tests.E2E.NG/tests/setup/improved-global-setup.ts` - Better global setup
- `test/Tests.E2E.NG/tests/setup/improved-global-teardown.ts` - Smart cleanup
- `test/Tests.E2E.NG/playwright-improved.config.ts` - Optimized Playwright config
- `test/Tests.E2E.NG/tests/improved-example.spec.ts` - Example tests

## Usage

### Running Tests with Improved Setup

```bash
# Run tests with improved server management
npm run test:improved

# Run with UI (headed mode)
npm run test:improved:headed

# Clean run (stops all servers before and after)
npm run test:improved:clean
```

### Environment Variables

- `PREWARM_SERVERS`: Pre-start servers before tests (default: true locally, false in CI)
- `CLEANUP_BEFORE_TESTS`: Kill existing servers before tests (default: false)
- `CLEANUP_AFTER_TESTS`: Stop servers after tests (default: false)
- `KILL_EXISTING_SERVERS`: Kill processes on conflicting ports (default: true)

## Benefits

### 1. No More Terminal Spam
- Servers run in detached mode with hidden windows
- Output redirected to pipes instead of spawning terminals
- Reduced logging to essential information only

### 2. Improved Performance
- Server reuse eliminates startup overhead
- Parallel test execution with proper isolation
- Faster test runs due to pre-warmed servers

### 3. Better Reliability
- Automatic retry on transient failures
- Circuit breaker prevents cascade failures
- Exponential backoff reduces server load
- Health checks ensure server availability

### 4. Resource Efficiency
- Automatic cleanup of old databases (> 1 hour)
- Reuse of proxy configurations
- Smart port allocation and conflict resolution
- State persistence across test runs

## Polly Policies Implemented

### HTTP Policies
1. **Retry Policy**: 5 retries with exponential backoff (2^n seconds + jitter)
2. **Circuit Breaker**: Opens after 3 failures, 30-second break duration
3. **Combined Policy**: Wraps retry around circuit breaker

### Database Policies
1. **Retry Policy**: 3 retries for transient database errors
2. **Circuit Breaker**: Opens after 5 failures, 60-second break duration
3. **Test Policy**: Faster retries (200ms * attempt) for test scenarios

### Test API Policies (TypeScript)
1. **Retry Logic**: 5 retries with exponential backoff + jitter
2. **Circuit Breaker**: 3 failure threshold, 10-second reset timeout
3. **Smart Retry Conditions**: Based on error type and HTTP status

## Architecture Improvements

### Server Lifecycle
```
1. Test requests server for parallel index N
2. Manager checks for existing healthy server
3. If healthy: Reuse existing server
4. If not: Create new server with resilience
5. Save state for future reuse
6. Health monitoring continues in background
```

### Resilience Flow
```
Request → Retry Policy → Circuit Breaker → Operation
   ↑                                           ↓
   └─────── Exponential Backoff ←─── Failure ─┘
```

## Migration Guide

To migrate existing tests to use the improved setup:

1. Update test imports:
```typescript
// Old
import { test, expect } from './setup/test-fixture';

// New
import { test, expect } from './setup/improved-test-fixture';
```

2. Update Playwright config reference:
```bash
# Old
npm test

# New
npm run test:improved
```

3. API calls automatically get resilience through ApiHelpers

## Monitoring and Debugging

### Server State
Check server state in temp directory:
- Location: `${TEMP}/crud-test-servers-state.json`
- Find your temp directory with: `echo $TEMP` (Windows) or `echo $TMPDIR` (Mac/Linux)

### Health Checks
Servers are monitored every 30 seconds. Check logs for health status:
```
♻️ Reusing healthy servers for parallel 0
✅ API health check passed
✅ Angular health check passed
```

### Troubleshooting
1. **Servers not starting**: Check port availability, increase timeouts
2. **Tests failing**: Check retry logs for transient issues
3. **Performance issues**: Adjust worker count and retry policies

## Best Practices

1. **Keep servers running** between test runs for faster execution
2. **Use `test:improved:clean`** only when necessary (CI or major changes)
3. **Monitor server health** through state file
4. **Adjust retry policies** based on infrastructure capabilities
5. **Use circuit breakers** to prevent cascade failures

## Future Enhancements

1. **Distributed testing** with server pool across machines
2. **Metrics collection** for retry/circuit breaker statistics
3. **Dynamic scaling** based on test load
4. **Advanced health checks** with performance metrics
5. **Integration with APM tools** for monitoring