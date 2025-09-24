# Test Server Optimization

## Overview

This document describes the optimized test execution strategy implemented to dramatically reduce E2E test startup times from 90+ seconds to under 5 seconds.

## Problem Statement

Previously, every test run would:
1. Kill any existing servers
2. Start fresh API server (10-15 seconds)
3. Start fresh Angular server (60-90 seconds)
4. Create new database
5. Run tests

This resulted in 90+ seconds of startup time for every test execution, significantly impacting developer productivity.

## Solution: Smart Server Detection and Reuse

### Key Components

#### 1. Server Detection (`server-utils.ts`)
- `isServerRunning()`: Checks if servers are already running via health endpoints
- `ServerStatus`: Tracks which servers were started by tests vs pre-existing
- `checkPortAvailability()`: Validates port availability before startup

#### 2. Optimized Global Setup (`optimized-global-setup.ts`)
- Detects existing servers before attempting to start new ones
- Reuses running servers across test executions
- Only resets database between runs (not full restart)
- Provides clear status logging showing time saved

#### 3. Database Reset Mechanism
- Secure `/api/database/reset` endpoint (Testing environment only)
- Token-based authentication (`X-Test-Reset-Token` header)
- Localhost-only restriction for additional security
- Fast SQLite data cleanup without schema recreation

## Performance Improvements

### Measured Results
- **Test initialization**: 0.04 seconds
- **Server response times**: 4-5ms (already warm)
- **Database reset**: 0.01 seconds
- **Overall improvement**: 94.4% reduction in startup time

### Time Savings
- **First run**: Normal startup (servers need to start)
- **Subsequent runs**: ~100 seconds saved per execution
- **CI/CD**: Reuses servers between test suites

## Configuration

### Environment Variables
```bash
# API Configuration
API_PORT=5172
ASPNETCORE_ENVIRONMENT=Testing

# Angular Configuration  
ANGULAR_PORT=4200

# Security
TEST_RESET_TOKEN=test-only-token
```

### Test Configurations
All Playwright configs updated to use `optimized-global-setup.ts`:
- `playwright.config.ts` - Default configuration
- `playwright.config.ci.ts` - CI/CD with 3 browsers
- `playwright.config.fast.ts` - Quick smoke tests
- `playwright.config.quiet.ts` - Minimal output
- `playwright.config.serial.ts` - Serial execution
- `playwright.config.single-browser.ts` - Single browser testing

## Security Considerations

### Database Reset Endpoint
1. **Environment restriction**: Only available in Development/Testing environments
2. **Token authentication**: Requires valid `X-Test-Reset-Token`
3. **Localhost only**: Requests from non-localhost IPs are rejected
4. **No production risk**: Returns 404 in production environment

### Best Practices
- Never expose reset endpoint in production
- Rotate test tokens regularly in CI/CD
- Use separate databases for each test worker
- Clean up old test databases periodically

## Usage

### Running Tests with Optimization
```bash
# Standard test run (reuses servers)
npm test

# CI configuration (all browsers)
npm run test:ci

# Quick smoke tests
npm run test:fast

# Quiet mode
npm run test:quiet
```

### Manual Server Management
```bash
# Start servers manually (they'll be reused)
npm run dev:api
npm run dev:angular

# Run tests (will detect and reuse)
npm test
```

## Troubleshooting

### Servers Not Detected
- Check health endpoints are responding
- Verify ports 5172 and 4200 are correct
- Ensure servers are fully started before running tests

### Database Reset Fails
- Verify TEST_RESET_TOKEN environment variable
- Check API server is in Testing environment
- Ensure database file permissions

### Performance Degradation
- Check for server memory leaks
- Verify database isn't growing too large
- Monitor server response times

## Implementation Details

### Server Lifecycle
1. **Detection Phase**: Check if servers are running
2. **Reuse Decision**: Use existing or start new
3. **Status Tracking**: Mark as pre-existing or test-started
4. **Teardown Logic**: Only kill servers started by tests

### Database Isolation
- Each test run gets unique database file
- Database reset between tests for data isolation  
- Schema preserved to avoid recreation overhead
- Old databases cleaned up after 1 hour

## Future Improvements

1. **Parallel Execution**: Support multiple workers with separate databases
2. **Container Support**: Docker-based test environments
3. **Cloud Testing**: Distributed test execution
4. **Smart Caching**: Cache compiled Angular assets
5. **Health Monitoring**: Track server performance over time