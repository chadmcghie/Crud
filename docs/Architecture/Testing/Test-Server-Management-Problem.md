# Test Server Management Problem Specification

## Problem Statement
E2E test execution is extremely slow due to unnecessary server restarts between test runs.

## Current State

### System Architecture
- **Frontend**: Angular application
- **Backend**: .NET API 
- **Database**: SQLite file-based database
- **Test Framework**: Playwright
- **Execution Model**: Serial (one test at a time)

### Current Behavior
Each test run follows this pattern:
```
Test Run 1: Kill servers → Start API (10s) → Start Angular (90s) → Run tests
Test Run 2: Kill servers → Start API (10s) → Start Angular (90s) → Run tests  
Test Run 3: Kill servers → Start API (10s) → Start Angular (90s) → Run tests
```

**Total overhead per test run**: ~90-100 seconds before tests even begin

## The Problem

### Primary Issue
Test execution is extremely slow due to unnecessary server restarts.

### Specific Problems
1. **Unnecessary Server Restarts**: Each test run kills and restarts both API and Angular servers
2. **Angular Compilation Time**: Angular server takes 60-90 seconds to compile and start
3. **API Startup Time**: API server takes 5-10 seconds to start
4. **Cumulative Delay**: Every test run incurs full startup penalty

### Impact
- Developer productivity severely impacted
- CI/CD pipeline takes excessive time
- Discourages running tests frequently
- Makes TDD/BDD practices impractical

## Desired Behavior

### Optimal Flow
```
First Run:  Start API (10s) → Start Angular (90s) → Run tests
Test Run 2: Reset database (1s) → Run tests
Test Run 3: Reset database (1s) → Run tests
```

### Key Principle
**Servers are stateless and should remain running. Only the database needs resetting between test runs.**

## Requirements

### Functional Requirements
1. **Data Isolation**: Each test run must start with a clean database
2. **Server Persistence**: API and Angular servers should stay running between test runs
3. **Fast Iteration**: Subsequent test runs should start in <5 seconds
4. **Backwards Compatibility**: Should work with existing test suites

### Non-Functional Requirements
1. **Performance**: Test startup time <5 seconds after initial run
2. **Reliability**: Database reset must be consistent and complete
3. **Security**: Database reset mechanism must not be exploitable
4. **Maintainability**: Solution should be simple and well-documented

## Constraints

### Technical Constraints
1. **SQLite Single Writer**: Cannot have multiple write connections to SQLite database
2. **File Locking**: Cannot delete database file while API has it open
3. **Process Isolation**: Cannot use distributed transactions across processes
4. **Schema Preservation**: Must maintain database schema between test runs

### Architectural Constraints
1. **Separate Processes**: API, Angular, and Tests run in separate processes
2. **HTTP Communication**: Tests communicate with API via HTTP
3. **Database Ownership**: API process owns the database connection

## Success Criteria

### Measurable Outcomes
- ✅ Test runs after the first one start in <5 seconds
- ✅ Database is completely clean for each test run
- ✅ No test data bleeds between runs
- ✅ Servers remain running between test runs
- ✅ Solution works on Windows, Linux, and Mac
- ✅ No security vulnerabilities introduced

### Validation Tests
1. Run test suite twice - second run should start in <5 seconds
2. Create data in test 1, verify it's gone in test 2
3. Verify servers stay running between test runs
4. Verify solution works in CI/CD environment

## Proposed Solutions

### Option 1: Database Reset Endpoint (Current Approach)
- **Pros**: Works with all constraints, simple to implement
- **Cons**: Security risk if not properly protected

### Option 2: Separate Database Per Test Run
- **Pros**: Complete isolation
- **Cons**: Requires API restart to switch databases

### Option 3: In-Memory Database for Tests
- **Pros**: Fast, no file locking issues
- **Cons**: Requires different configuration, may not match production

### Option 4: Database Snapshot/Restore
- **Pros**: Fast restore, maintains schema
- **Cons**: Complex implementation with SQLite

## Decision Required
Which approach best balances security, performance, and maintainability while working within our constraints?

## References
- [ADR-001: Serial E2E Testing](../Decisions/0001-Serial-E2E-Testing.md)
- [E2E Testing Strategy](./End%20To%20End%20Testing.md)
- [Database Configuration](../Database-Configuration.md)