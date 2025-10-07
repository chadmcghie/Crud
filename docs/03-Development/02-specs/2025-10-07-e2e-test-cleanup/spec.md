# Spec Requirements Document

> Spec: E2E Test Infrastructure Cleanup and Timer Elimination
> Created: 2025-10-07
> GitHub Issue: #279 - E2E Test Infrastructure Cleanup and Timer Elimination

## Overview

Refactor E2E test infrastructure to eliminate test-specific code pollution in Angular production files and replace timer-based waits with event-driven patterns. This refactoring will improve code maintainability, test reliability (especially in CI environments), and production security by removing test backdoors from the application codebase.

## User Stories

### Developer Experience Story

As a **frontend developer**, I want to work with clean Angular production code that has zero test awareness, so that I can maintain and extend the application without worrying about breaking test-specific logic or accidentally deploying test backdoors to production.

**Workflow:** When reviewing or modifying auth.service.ts, app.ts, or auth.guard.ts files, developers currently encounter 140+ lines of E2E detection logic, mock authentication, and test-specific attributes. This creates confusion about which code is production-critical versus test-only, increases cognitive load, and introduces security risks. After this refactoring, these files will contain only production logic, making them easier to understand, modify, and secure.

### Test Reliability Story

As a **QA engineer**, I want E2E tests that use event-driven waits instead of arbitrary timeouts, so that tests pass consistently in both local and CI environments without flakiness or false failures.

**Workflow:** Currently, 14 E2E test files use `waitForTimeout()` with hardcoded delays (ranging from 100ms to 2000ms), which work locally but fail in CI due to slower resource constraints. These timer-based waits cause intermittent failures that require manual retries and slow down the development pipeline. After implementing event-driven waits using Playwright's built-in capabilities, tests will wait for actual application state changes rather than arbitrary time periods, eliminating timing-related flakiness.

### Production Security Story

As a **security engineer**, I want authentication guards and services that contain no test bypass logic, so that the production application has no security backdoors that could be exploited if test flags are accidentally enabled.

**Workflow:** The current auth.guard.ts contains TestAuthService dependency that bypasses authentication when E2E mode is detected. While intended for testing, this creates a potential security vulnerability if the detection logic misfires or if test mode flags are inadvertently set in production. After refactoring, all authentication logic will be production-only, with test mocking handled entirely through Playwright's APIs outside the application code.

## Spec Scope

1. **Remove E2E Logic from auth.service.ts** - Delete 134 lines of `isE2ETestEnvironment()` detection logic, E2E auto-authentication, and localStorage test mode manipulation (lines 35-169).

2. **Clean app.ts Component** - Remove `data-e2e-ready` and `data-e2e-nav` attributes, `isE2EReady` property, and all debug console.log statements (~15 lines total).

3. **Simplify auth.guard.ts** - Remove TestAuthService dependency and E2E bypass logic, using only AuthService for authentication checks (~5 lines).

4. **Relocate test-auth.service.ts** - Move test-auth.service.ts from `src/Angular/src/app/` to test directory or delete if no longer needed.

5. **Create Test-Specific Authentication Helper** - Build new `test/Tests.E2E.NG/helpers/test-auth-setup.ts` using Playwright's `page.addInitScript()` and route mocking APIs to handle authentication in tests.

6. **Enhance page-helpers.ts** - Add event-driven wait methods: `waitForNavigationComplete()`, `waitForDataLoad()`, `waitForComponentReady()`, `waitForFormSubmission()`.

7. **Replace 14 Timer-Based Waits** - Convert all `waitForTimeout()` calls in test files (full-workflow.spec.ts, angular-ui/people.spec.ts, smoke.spec.ts, user-journeys, reliability-scenarios, password-reset) to use new event-driven wait methods.

## Out of Scope

- Backend API changes (no modifications to .NET API controllers or services)
- Test framework replacement (continuing to use Playwright, not switching to Cypress/etc.)
- Angular component refactoring beyond removing test attributes
- Adding new E2E test coverage (focus is on infrastructure cleanup)
- Performance optimization of existing tests (beyond reliability improvements from event-driven waits)
- Unit test or integration test modifications
- Changes to Playwright configuration files (playwright.config.ts)

## Expected Deliverable

1. **Zero Test Code in Production** - All files in `src/Angular/src/app/` contain only production logic with no E2E detection, mock authentication, or test-specific attributes.

2. **Event-Driven Waits Only** - All 14 instances of `waitForTimeout()` replaced with event-driven patterns that wait for actual application state changes (navigation complete, API responses, component initialization).

3. **Test Pass Locally and CI** - Full E2E test suite passes reliably in both local development environment and GitHub Actions CI without timing-related failures or flakiness.
