# Architecture Decision Record: E2E Testing Database Strategy

## Date: 2025-09-04

## Status: Accepted

## Context

We have been experiencing persistent SQLite database locking issues in our GitHub Actions CI/CD pipeline when running E2E tests with Playwright. Despite extensive troubleshooting (16+ documented attempts), the core issue remains: SQLite's file-based locking mechanism doesn't handle concurrent operations well in CI environments, particularly during database reset operations (`EnsureDeletedAsync`/`EnsureCreatedAsync`).

### Current Issues
1. **Database Locking**: Tests fail with "database is locked" after the first database reset in CI
2. **Complex Workarounds**: 300+ lines of custom server management code in `optimized-global-setup.ts`
3. **CI-Specific Failures**: Tests pass locally but consistently fail in GitHub Actions
4. **Maintenance Burden**: Ongoing complexity managing SQLite connection strings, WAL mode, mutexes, etc.

## Investigation Findings

### Root Cause Analysis
- **SQLite Limitations**: SQLite uses file-based locking which is problematic in CI environments
- **Entity Framework Core**: `EnsureDeleted()` operations timeout in CI but not locally
- **Concurrent Access**: Even with serial test execution, database file locking persists
- **Over-Engineering**: We've been implementing complex server management that Playwright already provides

### Playwright's Built-in Capabilities
Playwright provides a `webServer` configuration that automatically:
- Starts and stops servers
- Waits for server readiness
- Reuses existing servers when appropriate
- Handles cross-platform differences
- Manages multiple servers

We've been duplicating this functionality with 300+ lines of custom code.

## Decision Options

### Option 1: Playwright webServer with Unique SQLite Files (Recommended)
**Approach**: Use Playwright's built-in `webServer` config with unique database files per test run

**Pros**:
- Eliminates database locking issues
- Removes 300+ lines of custom code
- Works with existing SQLite infrastructure
- Simple implementation
- No additional dependencies

**Cons**:
- Multiple database files in CI (minor cleanup concern)
- Slightly more disk usage

**Implementation**:
```typescript
const testRunId = Date.now();
webServer: {
  env: {
    ConnectionStrings__DefaultConnection: `Data Source=test_${testRunId}.db`
  }
}
```

### Option 2: PostgreSQL for E2E Tests Only
**Approach**: Use PostgreSQL in Docker for E2E tests while keeping SQLite for development

**Pros**:
- Designed for concurrent access
- Better represents production scenarios
- Eliminates all locking issues
- Industry standard for testing

**Cons**:
- Requires Docker
- Additional complexity
- Different database between dev and test
- Migration differences

### Option 3: In-Memory SQLite
**Approach**: Use SQLite's `:memory:` mode for ephemeral databases

**Pros**:
- Fastest performance
- No file system issues
- Automatic cleanup

**Cons**:
- Can't inspect database after tests
- Some SQLite features unavailable
- Connection lifecycle complexity

### Option 4: MySQL with Docker
**Approach**: Switch to MySQL for all environments

**Pros**:
- Good concurrent access
- Wide tooling support

**Cons**:
- Requires significant code changes
- MySQL licensing considerations
- Less optimal for .NET ecosystem than PostgreSQL

### Option 5: Testcontainers
**Approach**: Use Testcontainers library for isolated database instances

**Pros**:
- Perfect test isolation
- Supports any database
- Industry best practice

**Cons**:
- Requires Docker
- Additional dependencies
- Learning curve

## Recommendation

Implement **Option 1** (Playwright webServer with Unique SQLite Files) first as it:
1. Requires minimal changes
2. Solves the immediate problem
3. Leverages Playwright's built-in features
4. Maintains current technology stack

If issues persist, implement **Option 2** (PostgreSQL) as a robust long-term solution.

## Implementation Plan

### Phase 1: Playwright webServer Migration
1. Configure `webServer` in `playwright.config.ts`
2. Remove custom server management code
3. Implement unique database naming strategy
4. Update CI workflow

### Phase 2: Database Cleanup Strategy
1. Add test fixtures for database reset
2. Implement cleanup in global teardown
3. Add file cleanup for old databases

### Phase 3: Monitoring & Validation
1. Run tests in CI to validate solution
2. Monitor for any locking issues
3. Document performance improvements

### Phase 4: (If Needed) PostgreSQL Migration
1. Add PostgreSQL Docker service to CI
2. Create PostgreSQL-specific connection strings
3. Update Entity Framework providers
4. Regenerate migrations for PostgreSQL

## Consequences

### Positive
- Eliminates database locking issues
- Reduces code complexity by 300+ lines
- Faster test execution
- Better maintainability
- Leverages framework capabilities

### Negative
- Multiple database files created (minor)
- Test commands appear more complex (but this complexity is necessary)
- Risk of future "simplification" breaking the tests again

### Mitigation
- Added deprecation warnings to problematic files
- Added explicit warnings in package.json  
- Documented in CLAUDE.md with clear DO NOT CHANGE warnings
- GitHub issue #79 tracks complete migration to eliminate problematic files

### Neutral
- Different approach from current implementation
- Requires configuration changes

## References
- [Playwright Web Server Documentation](https://playwright.dev/docs/test-webserver)
- [Entity Framework Core SQLite Limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
- [SQLite Database Locking](https://sqlite.org/lockingv3.html)
- ADR-001: Serial E2E Testing Decision
- Previous troubleshooting: `claude-task-e2e-test-serial-execution-fix-20250828.md`