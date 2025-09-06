# Spec Requirements Document

> Spec: E2E Test Simplification
> Created: 2025-08-29
> Status: COMPLETED

## Overview

Simplify the E2E test infrastructure to align with ADR-001 Serial E2E Testing Strategy by removing parallel execution complexity and implementing a single-worker, serial test execution model. This will improve test reliability from current 70% to target 100% while maintaining sub-10-minute execution times.

## User Stories

### Developer Running Tests Locally

As a developer, I want to run E2E tests with a single command, so that I can validate my changes without complex setup or configuration.

When I run `npm test` in the E2E test directory, the system should automatically start the required servers (API and Angular), run all tests serially with a single worker, and clean up resources when complete. The database should be automatically reset between test files to ensure isolation without the complexity of parallel worker management.

### CI/CD Pipeline Execution

As a DevOps engineer, I want E2E tests to run reliably in CI without flaky failures, so that we can trust our automated testing pipeline.

The CI pipeline should use the same serial execution configuration as local development, starting servers once at the beginning, running all tests sequentially, and providing clear pass/fail results. Test execution should complete within 10 minutes for the full suite and 2 minutes for smoke tests.

## Spec Scope

1. **Unified Serial Configuration** - Single playwright.config.ts with workers=1 and fullyParallel=false
2. **Simplified Server Management** - Basic global setup/teardown without persistent server reuse logic
3. **SQLite File-Based Cleanup** - Simple delete and recreate database file between test files
4. **CI Pipeline Alignment** - Use Playwright's globalSetup instead of manual server management
5. **Remove Parallel Complexity** - Eliminate all parallel execution code, worker isolation, and retry logic

## Out of Scope

- PostgreSQL or other database migrations
- Parallel test execution optimization
- In-memory database implementation
- Schema-based isolation strategies
- Cross-browser testing for all tests (only critical paths)
- Test execution time optimization beyond basic improvements

## Expected Deliverable

1. All E2E tests pass reliably 100% of the time with serial execution
2. Single `npm test` command runs all tests with automatic server management
3. CI pipeline executes tests in under 10 minutes with simplified configuration

## Implementation Notes

**Status: COMPLETED** - The E2E test simplification was successfully implemented using a different approach than originally outlined:

- Leveraged Playwright's built-in webServer configuration for automatic server management
- Achieved serial execution through workers=1 configuration in playwright.config.webserver.ts
- Database isolation implemented via unique database filenames per test run instead of file deletion
- CI/CD pipeline simplified to use standard Playwright commands with webServer
- All deliverables achieved: 100% test reliability, single command execution, and sub-10-minute CI runs