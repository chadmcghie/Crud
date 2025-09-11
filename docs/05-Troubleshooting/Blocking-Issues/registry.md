# Blocking Issues Registry

## Overview
Master registry of all blocking issues encountered in the project. This registry provides quick lookup for issue patterns and resolutions.

## Active Issues
| ID | Created | Spec | Category | Description | Severity |
|---|---|---|---|---|---|
| BI-2025-09-11-003 | 2025-09-11 | controller-authorization-protection | functionality | RowVersion concurrency control 409 Conflict in PUT_People_Should_Update_Person_Roles test | high |

## Resolved Issues
| ID | Created | Resolved | Spec | Category | Description | Resolution Summary |
|---|---|---|---|---|---|---|
| BI-2025-09-11-002 | 2025-09-11 | 2025-09-11 | 2025-09-10-api-response-caching | functionality | ConditionalRequestMiddleware ETag comparison logic failures causing 6 tests to be skipped | Fixed HTTP header API usage, middleware pipeline registration, and test environment configuration - all 6 tests now pass |
| BI-2025-09-09-002 | 2025-09-09 | 2025-09-09 | refactor-database-controller | test | Test delay anti-pattern in AuthInterceptor tests | Refactored to use fakeAsync/tick instead of setTimeout delays |
| BI-2025-09-09-001 | 2025-09-09 | 2025-09-09 | redis-caching-layer | test | Password reset tests database isolation failure | Fixed DatabaseTestService to clean Users and PasswordResetTokens tables |
| BI-2025-09-08-001 | 2025-09-08 | 2025-09-08 | redis-caching-layer | test | ICacheService DI registration missing in integration tests | Unified ICacheService interfaces between App and Infrastructure layers |
| BI-2025-08-30-001 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | Smoke test auth failure in CI - SQLite database path issues | Fixed TestDatabaseFactory to use current directory in CI, added 0.0.0.0 binding for Docker |
| BI-2025-08-30-002 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | E2E test timeouts and API connection failures | Fixed manual cleanup timeout, hardcoded URLs, and Docker networking issues |
| BI-2025-08-31-001 | 2025-08-31 | 2025-09-05 | test-server-optimization | test | E2E tests failing in CI - Docker networking issues | Resolved by using correct playwright.config.webserver.ts configuration with Playwright's built-in webServer feature |
| BI-2025-09-08-001 | 2025-09-08 | 2025-09-08 | N/A | build | MediatR 13 RequestHandlerDelegate compilation errors in tests | Fixed by adding CancellationToken parameter to test delegate lambdas |
| BI-2025-09-11-001 | 2025-09-11 | 2025-09-11 | controller-authorization-protection | test | Integration tests failing after authorization and middleware changes | Updated compression tests to use authenticated HTTP clients and proper test infrastructure |

## Common Patterns

### Authorization Test Failures
**Pattern**: Integration tests failing with 401 Unauthorized after controller authorization implementation
**Symptoms**: 
- Tests that previously passed now return HTTP 401
- Error: "Expected response.IsSuccessStatusCode to be true, but found False"
- Occurs when accessing protected endpoints without authentication

**Root Cause**: Tests using unauthenticated HTTP clients to access endpoints that now require authorization

**Solution**: 
- Update tests to use `AuthenticationTestHelper.CreateUserClientAsync()` or `CreateAdminClientAsync()`
- Ensure tests use consistent test infrastructure (`SqliteTestWebApplicationFactory`)
- Create authenticated clients before making requests to protected endpoints

**Prevention**:
- When adding authorization to controllers, audit all integration tests that access those endpoints
- Use consistent test infrastructure across all integration test classes
- Document authorization requirements in test setup guides

## Statistics
- Total Issues: 11
- Active: 1
- Resolved: 10
- Average Resolution Time: ~3.5 hours