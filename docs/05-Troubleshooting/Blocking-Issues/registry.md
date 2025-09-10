# Blocking Issues Registry

## Overview
Master registry of all blocking issues encountered in the project. This registry provides quick lookup for issue patterns and resolutions.

## Active Issues
| ID | Created | Spec | Category | Description | Severity |
|---|---|---|---|---|---|
(No active issues)

## Resolved Issues
| ID | Created | Resolved | Spec | Category | Description | Resolution Summary |
|---|---|---|---|---|---|---|
| BI-2025-09-09-002 | 2025-09-09 | 2025-09-09 | refactor-database-controller | test | Test delay anti-pattern in AuthInterceptor tests | Refactored to use fakeAsync/tick instead of setTimeout delays |
| BI-2025-09-09-001 | 2025-09-09 | 2025-09-09 | redis-caching-layer | test | Password reset tests database isolation failure | Fixed DatabaseTestService to clean Users and PasswordResetTokens tables |
| BI-2025-09-08-001 | 2025-09-08 | 2025-09-08 | redis-caching-layer | test | ICacheService DI registration missing in integration tests | Unified ICacheService interfaces between App and Infrastructure layers |
| BI-2025-08-30-001 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | Smoke test auth failure in CI - SQLite database path issues | Fixed TestDatabaseFactory to use current directory in CI, added 0.0.0.0 binding for Docker |
| BI-2025-08-30-002 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | E2E test timeouts and API connection failures | Fixed manual cleanup timeout, hardcoded URLs, and Docker networking issues |
| BI-2025-08-31-001 | 2025-08-31 | 2025-09-05 | test-server-optimization | test | E2E tests failing in CI - Docker networking issues | Resolved by using correct playwright.config.webserver.ts configuration with Playwright's built-in webServer feature |
| BI-2025-09-08-001 | 2025-09-08 | 2025-09-08 | N/A | build | MediatR 13 RequestHandlerDelegate compilation errors in tests | Fixed by adding CancellationToken parameter to test delegate lambdas |

## Common Patterns
Document recurring issue patterns here as they emerge.

## Statistics
- Total Issues: 8
- Active: 0
- Resolved: 8
- Average Resolution Time: ~4 hours